//
// EditPackagesDialog.cs: Allows you to add and remove pkg-config packages to the project
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using AppKit;
using Foundation;
using NuGet.Protocol.Core.Types;
using CoreGraphics;

namespace CBinding
{
	// Delegates recieve events associated with user action and determine how an item should be visualized
	class TableDelegate : NSTableViewDelegate
	{
		const string identifer = "myCellIdentifier";

        public Action<NSTableView, PackageItem> OnColumnChecked { get; set; }

		// Returns the NSView for a given column/row. NSTableView is strange as unlike NSOutlineView 
		// it does not pass in the data for the given item (obtained from the DataSource) for the NSView APIs
		public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
		{
            var dataSource = (TableDataSource)tableView.DataSource;
            var packageItem = dataSource.Items [(int)row];
          
			NSView view = null;

			if (packageItem == null)
				return view;

			if (tableColumn.Title == "") {
                NSButton checkBox = (NSButton)tableView.MakeView (identifer, this);
                if (checkBox == null) {
                    checkBox = new NSButton () { TranslatesAutoresizingMaskIntoConstraints = false, Title = String.Empty };
                    checkBox.SetButtonType (NSButtonType.Switch);
                }
                view = checkBox;

                packageItem.IsRowChecked = packageItem.IsRowChecked;
				checkBox.State = packageItem.IsRowChecked ? NSCellStateValue.On : NSCellStateValue.Off;

                checkBox.Activated += (s, e) => {
                    packageItem.IsRowChecked = checkBox.State == NSCellStateValue.On;
					OnColumnChecked?.Invoke (tableView, packageItem);
				};

			} else {
				NSTextField label = (NSTextField)tableView.MakeView (identifer, this);
				if (label == null) {
					label = NSTextField.CreateLabel (string.Empty);
					label.TranslatesAutoresizingMaskIntoConstraints = false;
					label.Identifier = identifer;
				}

                if (tableColumn.Title == "Package") {
                    label.StringValue = packageItem.Name;
				} else if (tableColumn.Title == "Version") {
					label.StringValue = packageItem.Version;
				} else {
                    throw new Exception ("");
                }

				view = label;
			}
			
            //if (tableColumn.Identifier == "Values")
            //	view.StringValue = (NSString)row.ToString ();
            //else
            //	view.StringValue = (NSString)NumberWords [row];

            return view;
		}

		// An example of responding to user input 
		public override bool ShouldSelectRow (NSTableView tableView, nint row)
		{
			return true;
		}
	}

	class PackageItem
	{
		public string Name { get; set; }
		public string Version { get; set; }
		public bool IsInProject { get; set; }

		public bool IsRowChecked { get; set; }


        public bool HasChanged => IsInProject != IsRowChecked;

	}

	// Data sources in general walk a given data source and respond to questions from AppKit to generate
	// the data used in your Delegate. However, as noted in GetViewForItem above, NSTableView
	// only requires the row count from the data source, instead of also requesting the data for that item
	// and passing that into the delegate.
	class TableDataSource : NSTableViewDataSource
	{
        public Func<List<PackageItem>> ItemsFunc { get; set; }
        public List<PackageItem> Items => ItemsFunc ();
		public override nint GetRowCount (NSTableView tableView) => Items.Count;
	}

	public class EditPackagesDialogViewController : NSViewController
    {
        private CProject project;
        private ProjectPackageCollection selectedPackages = new ProjectPackageCollection ();
        private List<Package> packagesOfProjects;
        private List<Package> packages = new List<Package> ();

        // Column IDs
        const int NormalPackageToggleID = 0;
        const int NormalPackageNameID = 1;
        const int NormalPackageVersionID = 2;

        const int ProjectPackageToggleID = 0;
        const int ProjectPackageNameID = 1;
        const int ProjectPackageVersionID = 2;

        const int SelectedPackageNameID = 0;
        const int SelectedPackageVersionID = 1;

		List<PackageItem> projectPackageListStore = new List<PackageItem> ();
		List<PackageItem> normalPackageListStore = new List<PackageItem> ();
		List<PackageItem> selectedPackageListStore = new List<PackageItem> ();

		NSTableView normalPackageTreeView, projectPackageTreeView;
		NSTabView notebook1;

        NSStackView CreateVerticalStackView()
        {
            return new NSStackView () {
				TranslatesAutoresizingMaskIntoConstraints = false,
				Alignment = NSLayoutAttribute.Leading,
				Orientation = NSUserInterfaceLayoutOrientation.Vertical,
				Distribution = NSStackViewDistribution.Fill
			};
		}

		NSStackView CreateHorizontalStackView ()
		{
			return new NSStackView () {
				TranslatesAutoresizingMaskIntoConstraints = false,
				Alignment = NSLayoutAttribute.CenterX,
				Orientation = NSUserInterfaceLayoutOrientation.Horizontal,
				Distribution = NSStackViewDistribution.Fill
			};
		}

        NSView CreateNormalPackagesContentPage()
        {
			var normalContentStackView = CreateVerticalStackView ();
			var scrollView = new NSScrollView () {
				TranslatesAutoresizingMaskIntoConstraints = false,
				HasVerticalScroller = true
			};
			normalContentStackView.AddArrangedSubview (scrollView);
			scrollView.LeadingAnchor.ConstraintEqualTo (normalContentStackView.LeadingAnchor).Active = true;
			scrollView.TrailingAnchor.ConstraintEqualTo (normalContentStackView.TrailingAnchor).Active = true;

			normalPackageTreeView = new NSTableView ();
			scrollView.DocumentView = normalPackageTreeView;

			// Just like NSOutlineView, NSTableView expects at least one column
			normalPackageTreeView.AddColumn (new NSTableColumn ("Check") { Title = "", Width = 50 });
			normalPackageTreeView.AddColumn (new NSTableColumn ("Package") { Title = "Package", Width = 300 });
			normalPackageTreeView.AddColumn (new NSTableColumn ("Version") { Title = "Version", Width = 300 });

			// Setup the Delegate/DataSource instances to be interrogated for data and view information
			// In Unified, these take an interface instead of a base class and you can combine these into
			// one instance. 
			normalPackageTreeView.DataSource = new TableDataSource () { ItemsFunc = () => normalPackageListStore };
			normalPackageTreeView.Delegate = new TableDelegate () {
                 OnColumnChecked = (NSTableView s,PackageItem packageItem) => {
                     OnNormalPackageToggled (s, packageItem);
				 }
            };

			// <!-- Normal packages -->

			var normalDetailsView = CreateHorizontalStackView ();
			normalDetailsView.SetHuggingPriority ((int)NSLayoutPriority.DefaultHigh, NSLayoutConstraintOrientation.Vertical);
			normalContentStackView.AddArrangedSubview (normalDetailsView);

			normalDetailsView.AddArrangedSubview (new NSView ());
			var normalDetails = new NSButton () { Title = "Details" };
			normalDetailsView.AddArrangedSubview (normalDetails);
			normalDetails.Activated += (s,e) => OnDetailsButtonClicked (null, EventArgs.Empty);

			return normalContentStackView;
		}

		NSView CreateSystemPackagesContentPage ()
		{
			var projectContentStackView = CreateVerticalStackView ();

			var projectScrollView = new NSScrollView () {
				TranslatesAutoresizingMaskIntoConstraints = false,
				HasVerticalScroller = true
			};
		    projectContentStackView.AddArrangedSubview (projectScrollView);
			projectScrollView.LeadingAnchor.ConstraintEqualTo (projectContentStackView.LeadingAnchor).Active = true;
			projectScrollView.TrailingAnchor.ConstraintEqualTo (projectContentStackView.TrailingAnchor).Active = true;

			projectPackageTreeView = new NSTableView ();
			projectScrollView.DocumentView = projectPackageTreeView;

			// Just like NSOutlineView, NSTableView expects at least one column
			projectPackageTreeView.AddColumn (new NSTableColumn ("Check") { Title = "", Width = 50 });
			projectPackageTreeView.AddColumn (new NSTableColumn ("Package") { Title = "Package", Width = 400 });
			projectPackageTreeView.AddColumn (new NSTableColumn ("Version") { Title = "Version", Width = 400 });

			// Setup the Delegate/DataSource instances to be interrogated for data and view information
			// In Unified, these take an interface instead of a base class and you can combine these into
			// one instance. 
			projectPackageTreeView.DataSource = new TableDataSource () { ItemsFunc = () => projectPackageListStore };
			projectPackageTreeView.Delegate = new TableDelegate () {
                OnColumnChecked = (NSTableView tableView,PackageItem packageItem) =>
                {
					OnProjectPackageToggled (tableView, packageItem);
				}
            };

			// <!-- Project packages -->

			var normalDetailsView = CreateHorizontalStackView ();
			normalDetailsView.SetHuggingPriority ((int)NSLayoutPriority.DefaultHigh, NSLayoutConstraintOrientation.Vertical);
			projectContentStackView.AddArrangedSubview (normalDetailsView);

			normalDetailsView.AddArrangedSubview (new NSView ());
			var normalDetails = new NSButton () { Title = "Details" };
			normalDetailsView.AddArrangedSubview (normalDetails);

            normalDetails.Activated += (s,e) => OnDetailsButtonClicked (null, EventArgs.Empty);

			return projectContentStackView;
		}

		public EditPackagesDialogViewController (CProject project)
        {
            this.project = project;

            selectedPackages.Project = project;
            selectedPackages.AddRange (project.Packages);

            var stackView = CreateVerticalStackView ();
            View = stackView;

			//first tabview

			notebook1 = new NSTabView () { TranslatesAutoresizingMaskIntoConstraints = false };
            stackView.AddArrangedSubview (notebook1);

            var normalPage = new NSTabViewItem () { Label = "System Packages" };
            notebook1.Add (normalPage);
            normalPage.View = CreateNormalPackagesContentPage ();

			// Project Packages

			var projectPage = new NSTabViewItem () { Label = "Project Packages" };
			notebook1.Add (projectPage);
            projectPage.View = CreateSystemPackagesContentPage ();

			//Gtk.CellRendererText textRenderer = new Gtk.CellRendererText ();

			//CellRendererImage pixbufRenderer = new CellRendererImage ();
			//pixbufRenderer.StockId = "md-package";

			//normalPackageListStore.DefaultSortFunc = NormalPackageCompareNodes;
			//projectPackageListStore.DefaultSortFunc = ProjectPackageCompareNodes;
			//selectedPackageListStore.DefaultSortFunc = SelectedPackageCompareNodes;

			//normalPackageListStore.SetSortColumnId (NormalPackageNameID, Gtk.SortType.Ascending);
			//projectPackageListStore.SetSortColumnId (ProjectPackageNameID, Gtk.SortType.Ascending);
			//selectedPackageListStore.SetSortColumnId (SelectedPackageNameID, Gtk.SortType.Ascending);

			//normalPackageTreeView.SearchColumn = NormalPackageNameID;
			//projectPackageTreeView.SearchColumn = ProjectPackageNameID;
			//selectedPackageTreeView.SearchColumn = SelectedPackageNameID;

			// <!-- Selected packages -->

			//Gtk.TreeViewColumn selectedPackageColumn = new Gtk.TreeViewColumn ();
			//selectedPackageColumn.Title = "Package";
			//selectedPackageColumn.PackStart (pixbufRenderer, false);
			//selectedPackageColumn.PackStart (textRenderer, true);
			//selectedPackageColumn.AddAttribute (textRenderer, "text", SelectedPackageNameID);

			//selectedPackageTreeView.Model = selectedPackageListStore;
			//selectedPackageTreeView.HeadersVisible = true;
			//selectedPackageTreeView.AppendColumn (selectedPackageColumn);
			//selectedPackageTreeView.AppendColumn ("Version", textRenderer, "text", SelectedPackageVersionID);

			// Fill up the project tree view
			packagesOfProjects = GetPackagesOfProjects (project);

            foreach (Package p in packagesOfProjects) {
                if (p.Name == project.Name) continue;

                packages.Add (p);
                string version = p.Version;
                bool inProject = selectedPackages.Contains (p);

                if (!IsPackageInStore (projectPackageListStore, p.Name, version)) {
					var packageItem = new PackageItem () {
						IsInProject = inProject, Version = version, Name = p.Name
					};
					projectPackageListStore.Add (packageItem);

                    if (inProject)
                        selectedPackageListStore.Add (packageItem);
                }
            }

            // Fill up the normal tree view
            foreach (string dir in ScanDirs ()) {
                if (Directory.Exists (dir)) {
                    DirectoryInfo di = new DirectoryInfo (dir);
                    FileInfo [] availablePackages = di.GetFiles ("*.pc");

                    foreach (FileInfo f in availablePackages) {
                        if (!IsValidPackage (f.FullName)) {
                            continue;
                        }

                        Package package = new Package (f.FullName);

                        packages.Add (package);

                        string name = package.Name;
                        string version = package.Version;
                        bool inProject = selectedPackages.Contains (package);

                        if (!IsPackageInStore (normalPackageListStore, name, version)) {
                            var packageItem = new PackageItem () {
                                IsInProject = inProject, Version = version, Name = name
                            };

							normalPackageListStore.Add (packageItem);

                            if (inProject)
                                selectedPackageListStore.Add(packageItem);
                        }
                    }
                }
            }

			normalPackageTreeView.ReloadData ();
            projectPackageTreeView.ReloadData ();
		}

		private List<Package> GetPackagesOfProjects (Project project)
        {
            List<Package> packages = new List<Package> ();
            Package package;

            foreach (SolutionFolderItem c in project.ParentFolder.Items) {
                if (null != c && c is CProject) {
                    CProject cproj = (CProject)c;
                    CProjectConfiguration conf = (CProjectConfiguration)cproj.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
                    if (conf.CompileTarget != CompileTarget.Exe) {
                        cproj.WriteMDPkgPackage (conf.Selector);
                        package = new Package (cproj);
                        packages.Add (package);
                    }
                }
            }

            return packages;
        }

        private bool IsPackageInStore (List<PackageItem> store, string pname, string pversion)
        {
            return store.Any (s => s.Name == pname && s.Version == pversion);

            //Gtk.TreeIter search_iter;
            //bool has_elem = store.GetIterFirst (out search_iter);

            //if (has_elem) {
            //    while (true) {
            //        string name = (string)store.GetValue (search_iter, pname_col);
            //        string version = (string)store.GetValue (search_iter, pversion_col);

            //        if (name == pname && version == pversion)
            //            return true;

            //        if (!store.IterNext (ref search_iter))
            //            break;
            //    }
            //}

            //return false;
        }

        private string [] ScanDirs ()
        {
            List<string> dirs = new List<string> ();
            string pkg_var = Environment.GetEnvironmentVariable ("PKG_CONFIG_PATH");
            string [] pkg_paths;

            dirs.Add ("/usr/lib/pkgconfig");
            dirs.Add ("/usr/lib64/pkgconfig");
            dirs.Add ("/usr/share/pkgconfig");
            dirs.Add ("/usr/local/lib/pkgconfig");
            dirs.Add ("/usr/local/share/pkgconfig");
            dirs.Add ("/usr/lib/x86_64-linux-gnu/pkgconfig");

            if (pkg_var == null) return dirs.ToArray ();

            pkg_paths = pkg_var.Split (':');

            foreach (string dir in pkg_paths) {
                if (string.IsNullOrEmpty (dir))
                    continue;
                string dirPath = System.IO.Path.GetFullPath (dir);
                if (!dirs.Contains (dirPath) && !string.IsNullOrEmpty (dir)) {
                    dirs.Add (dir);
                }
            }

            return dirs.ToArray ();
        }

        private void OnOkButtonClick (object sender, EventArgs e)
        {
            // Use this instead of clear, since clear seems to not update the packages tree
            while (project.Packages.Count > 0) {
                project.Packages.RemoveAt (0);
            }

            project.Packages.AddRange (selectedPackages);

        }

        private void OnCancelButtonClick (object sender, EventArgs e)
        {
        }

        private void OnRemoveButtonClick (object sender, EventArgs e)
        {
            //selectedPackageTreeView.Selection.GetSelected (out iter);

            //if (!selectedPackageListStore.IterIsValid (iter)) return;

            //string package = (string)selectedPackageListStore.GetValue (iter, SelectedPackageNameID);
            //bool isProject = false;

            //foreach (Package p in selectedPackages) {
            //    if (p.Name == package) {
            //        isProject = p.IsProject;
            //        selectedPackages.Remove (p);
            //        break;
            //    }
            //}

            //selectedPackageListStore.Remove (ref iter);

            //if (!isProject) {
            //    Gtk.TreeIter search_iter;
            //    bool has_elem = normalPackageListStore.GetIterFirst (out search_iter);

            //    if (has_elem) {
            //        while (true) {
            //            string current = (string)normalPackageListStore.GetValue (search_iter, NormalPackageNameID);

            //            if (current.Equals (package)) {
            //                normalPackageListStore.SetValue (search_iter, NormalPackageToggleID, false);
            //                break;
            //            }

            //            if (!normalPackageListStore.IterNext (ref search_iter))
            //                break;
            //        }
            //    }
            //} else {
            //    Gtk.TreeIter search_iter;
            //    bool has_elem = projectPackageListStore.GetIterFirst (out search_iter);

            //    if (has_elem) {
            //        while (true) {
            //            string current = (string)projectPackageListStore.GetValue (search_iter, ProjectPackageNameID);

            //            if (current.Equals (package)) {
            //                projectPackageListStore.SetValue (search_iter, ProjectPackageToggleID, false);
            //                break;
            //            }

            //            if (!projectPackageListStore.IterNext (ref search_iter))
            //                break;
            //        }
            //    }
            //}
        }

        private void OnNormalPackageToggled (object sender, PackageItem current)
        {
            if (current.HasChanged) {

                if (current.IsRowChecked) {

					selectedPackageListStore.Add (current);

					foreach (Package package in packages) {
						if (package.Name == current.Name && package.Version == current.Version) {
							selectedPackages.Add (package);
							break;
						}
					}

				} else {
					foreach (var element in selectedPackageListStore.ToArray ()) {
						selectedPackageListStore.Remove (element);
						foreach (Package p in selectedPackages) {
							if (p.Name == current.Name) {
								selectedPackages.Remove (p);
								break;
							}
						}
					}
				}
            } 
        }

		private void OnProjectPackageToggled (object sender, PackageItem packageItem)
        {
            //Gtk.TreeIter iter;
            //bool old = true;
            //string name;
            //string version;

            //if (projectPackageListStore.GetIter (out iter, new Gtk.TreePath (args.Path))) {
            //    old = (bool)projectPackageListStore.GetValue (iter, ProjectPackageToggleID);
            //    projectPackageListStore.SetValue (iter, ProjectPackageToggleID, !old);
            //}

            //name = (string)projectPackageListStore.GetValue (iter, ProjectPackageNameID);
            //version = (string)projectPackageListStore.GetValue (iter, ProjectPackageVersionID);

            //if (old == false) {
            //    selectedPackageListStore.AppendValues (name, version);

            //    foreach (Package p in packagesOfProjects) {
            //        if (p.Name == name) {
            //            selectedPackages.Add (p);
            //            break;
            //        }
            //    }
            //} else {
            //    Gtk.TreeIter search_iter;
            //    bool has_elem = selectedPackageListStore.GetIterFirst (out search_iter);

            //    if (has_elem) {
            //        while (true) {
            //            string current = (string)selectedPackageListStore.GetValue (search_iter, SelectedPackageNameID);

            //            if (current.Equals (name)) {
            //                selectedPackageListStore.Remove (ref search_iter);
            //                foreach (Package p in selectedPackages) {
            //                    if (p.Name == name) {
            //                        selectedPackages.Remove (p);
            //                        break;
            //                    }
            //                }

            //                break;
            //            }

            //            if (!selectedPackageListStore.IterNext (ref search_iter))
            //                break;
            //        }
            //    }
            //}
        }

        private bool IsValidPackage (string package)
        {
            bool valid = false;
            try {
                using (StreamReader reader = new StreamReader (package)) {
                    string line;

                    while ((line = reader.ReadLine ()) != null) {
                        if (line.StartsWith ("Libs:", true, null) && line.Contains (" -l")) {
                            valid = true;
                            break;
                        }
                    }
                }
            } catch {
                // Invalid file, permission error, broken symlink
            }

            return valid;
        }

        //int NormalPackageCompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
        //{
        //    string name1 = (string)model.GetValue (a, NormalPackageNameID);
        //    string name2 = (string)model.GetValue (b, NormalPackageNameID);
        //    return string.Compare (name1, name2, true);
        //}

        //int ProjectPackageCompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
        //{
        //    string name1 = (string)model.GetValue (a, ProjectPackageNameID);
        //    string name2 = (string)model.GetValue (b, ProjectPackageNameID);
        //    return string.Compare (name1, name2, true);
        //}

        //int SelectedPackageCompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
        //{
        //    string name1 = (string)model.GetValue (a, SelectedPackageNameID);
        //    string name2 = (string)model.GetValue (b, SelectedPackageNameID);
        //    return string.Compare (name1, name2, true);
        //}

        protected virtual void OnSelectedPackagesTreeViewCursorChanged (object sender, System.EventArgs e)
        {
            //removeButton.Sensitive = true;
        }

        protected virtual void OnRemoveButtonClicked (object sender, System.EventArgs e)
        {
            //removeButton.Sensitive = false;
        }

		protected virtual void OnDetailsButtonClicked (object sender, System.EventArgs e)
        {
            //Gtk.TreeIter iter;

            var activeTab = notebook1.Selected;
			
			//Gtk.Widget active_tab = notebook1.Children [notebook1.Page];
			string tab_label = activeTab.Label; // notebook1.GetTabLabelText (active_tab);
            string name = string.Empty;
            string version = string.Empty;
            Package package = null;

            if (tab_label == "System Packages") {
				var tmp = normalPackageListStore [(int)normalPackageTreeView.SelectedRow];
                name = tmp.Name;
                version = tmp.Version;
            } else if (tab_label == "Project Packages") {
				var tmp = projectPackageListStore [(int)projectPackageTreeView.SelectedRow];
				name = tmp.Name;
				version = tmp.Version;
			} else {
                return;
            }

            foreach (Package p in packages) {
                if (p.Name == name && p.Version == version) {
                    package = p;
                    break;
                }
            }

            if (package == null)
                return;

            PackageDetailsViewController details = new PackageDetailsViewController (package);
            NSWindow window = NSWindow.GetWindowWithContentViewController (details);
            var size = new CGSize (600, 400);
            window.MinSize = size;
			window.SetContentSize (size);

			window.Title = "Package Details";
            MonoDevelop.Ide.MessageService.RunCustomDialog (window);


			//details.Modal = true;
			//details.Show ();
		}

        protected virtual void OnNonSelectedPackageCursorChanged (object o, EventArgs e)
        {
            //Gtk.TreeIter iter;
            //Gtk.Widget active_tab = notebook1.Children [notebook1.Page];
            //Gtk.Widget active_label = notebook1.GetTabLabel (active_tab);

            //bool sensitive = false;

            //if (active_label == this.labelSystemPackages) {
            //    normalPackageTreeView.Selection.GetSelected (out iter);
            //    if (normalPackageListStore.IterIsValid (iter))
            //        sensitive = true;
            //} else if (active_label == this.labelProjectPackages) {
            //    projectPackageTreeView.Selection.GetSelected (out iter);
            //    if (projectPackageListStore.IterIsValid (iter))
            //        sensitive = true;
            //} else {
            //    return;
            //}

            //detailsButton.Sensitive = sensitive;
        }

        protected virtual void OnNotebook1SwitchPage (object o, Gtk.SwitchPageArgs args)
        {
            //Gtk.TreeIter iter;
            //Gtk.Widget active_tab = notebook1.Children [notebook1.Page];

            //switch (notebook1.GetTabLabelText (active_tab)) {
            //case "System Packages":
            //    normalPackageTreeView.Selection.GetSelected (out iter);
            //    detailsButton.Sensitive = normalPackageListStore.IterIsValid (iter);
            //    break;

            //case "Project Packages":
            //    projectPackageTreeView.Selection.GetSelected (out iter);
            //    detailsButton.Sensitive = projectPackageListStore.IterIsValid (iter);
            //    break;
            //}
        }
    }
}
