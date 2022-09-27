using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Collections;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;

namespace CBinding
{
	public class SystemFile : MonoDevelop.Ide.Gui.Pads.ProjectPad.SystemFile
	{
		public SystemFile (FilePath absolutePath, WorkspaceObject parent) : base (absolutePath, parent)
		{
		}

		public SystemFile (FilePath absolutePath, WorkspaceObject parent, bool showTransparent) : base (absolutePath, parent, showTransparent)
		{
		}
	}
}

namespace CBinding.ProjectPad
{
    class SystemFileNodeBuilder : TypeNodeBuilder
    {
        public override Type NodeDataType {
            get { return typeof (SystemFile); }
        }

        public override Type CommandHandlerType {
            get { return typeof (SystemFileNodeCommandHandler); }
        }

        public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
        {
            return Path.GetFileName (((SystemFile)dataObject).Name);
        }

        public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
        {
            attributes |= NodeAttributes.AllowRename;
        }

        public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
        {
            SystemFile file = (SystemFile)dataObject;
            nodeInfo.Label = file.Name;

            nodeInfo.Icon = IdeServices.DesktopService.GetIconForFile (file.Path, Xwt.IconSize.Small);

            if (file.ShowTransparent) {
                var gicon = Context.GetComposedIcon (nodeInfo.Icon, "fade");
                if (gicon == null) {
                    gicon = nodeInfo.Icon;
                    Context.CacheComposedIcon (nodeInfo.Icon, "fade", gicon);
                }
                nodeInfo.Icon = gicon;
                nodeInfo.Style = NodeInfo.LabelStyle.Disabled;
            }
        }
    }

    class SystemFileNodeCommandHandler : NodeCommandHandler
	{
		public override void OnRenameStarting (ref int selectionStart, ref int selectionLength)
		{
			string name = CurrentNode.NodeName;
			selectionStart = 0;
			selectionLength = Path.GetFileNameWithoutExtension (name).Length;
		}

		public override void RenameItem (string newName)
		{
            var file = (SystemFile)CurrentNode.DataItem;
            if (RenameFileWithConflictCheck (file.Path, newName, out string newPath)) {
				Console.WriteLine ("");
                //file.Path = newPath;
            }
        }

		internal static bool ContainsDirectorySeparator (string name)
		{
			return name.Contains (Path.DirectorySeparatorChar) || name.Contains (Path.AltDirectorySeparatorChar);
		}

		public static bool RenameFileWithConflictCheck (FilePath oldPath, string newName, out string newPath)
		{
			newPath = oldPath.ParentDirectory.Combine (newName);
			if (oldPath == newPath) {
				return false;
			}
			try {
                if (!FileService.IsValidPath (newPath) || ContainsDirectorySeparator (newName)) {
                    MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
                } else if (File.Exists (newPath) || Directory.Exists (newPath)) {
                    MessageService.ShowWarning (GettextCatalog.GetString ("File or directory name is already in use. Please choose a different one."));
                } else {
                    FileService.RenameFile (oldPath, newPath);
                    return true;
                }
            } catch (ArgumentException) { // new file name with wildcard (*, ?) characters in it
				MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
			} catch (IOException ex) {
				MessageService.ShowError (GettextCatalog.GetString ("There was an error renaming the file."), ex);
			}
			return false;
		}

		public override void ActivateItem ()
		{
			SystemFile file = CurrentNode.DataItem as SystemFile;
			IdeApp.Workbench.OpenDocument (file.Path, project: null);
		}

		public override void DeleteMultipleItems ()
		{
			if (CurrentNodes.Length == 1) {
				SystemFile file = (SystemFile)CurrentNodes [0].DataItem;
				if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to permanently delete the file {0}?", file.Path), AlertButton.Delete))
					return;
			} else {
				if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to permanently delete all selected files?"), AlertButton.Delete))
					return;
			}
			foreach (SystemFile file in CurrentNodes.Select (n => (SystemFile)n.DataItem)) {
				try {
					FileService.DeleteFile (file.Path);
				} catch {
					MessageService.ShowError (GettextCatalog.GetString ("The file {0} could not be deleted", file.Path));
				}
			}
		}

		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy | DragOperation.Move;
		}

		[CommandHandler (ProjectCommands.IncludeToProject)]
		[AllowMultiSelection]
		public void IncludeFileToProject ()
		{
			Set<IWorkspaceFileObject> projects = new Set<IWorkspaceFileObject> ();
			var nodesByProject = CurrentNodes.GroupBy (n => n.GetParentDataItem (typeof (Project), true) as Project);

			foreach (var projectGroup in nodesByProject) {
				Project project = projectGroup.Key;
				List<FilePath> newFiles = new List<FilePath> ();
				foreach (ITreeNavigator node in projectGroup) {
					SystemFile file = (SystemFile)node.DataItem;
					if (project != null) {
						newFiles.Add (file.Path);
						projects.Add (project);
					} else {
						SolutionFolder folder = node.GetParentDataItem (typeof (SolutionFolder), true) as SolutionFolder;
						if (folder != null) {
							folder.Files.Add (file.Path);
							projects.Add (folder.ParentSolution);
						} else {
							Solution sol = node.GetParentDataItem (typeof (Solution), true) as Solution;
							sol.RootFolder.Files.Add (file.Path);
							projects.Add (sol);
						}
					}
				}
				if (newFiles.Count > 0)
					project.AddFiles (newFiles);
			}
			IdeApp.ProjectOperations.SaveAsync (projects);
		}

		[CommandUpdateHandler (ProjectCommands.IncludeToProject)]
		public void UpdateIncludeFileToProject (CommandInfo info)
		{
			Project project = CurrentNode.GetParentDataItem (typeof (Project), true) as Project;
			if (project != null)
				return;
			if (CurrentNode.GetParentDataItem (typeof (Solution), true) != null) {
				info.Text = GettextCatalog.GetString ("Include to Solution");
				return;
			}
			info.Visible = false;
		}

		[CommandHandler (ViewCommands.OpenWithList)]
		public void OnOpenWith (object ob)
		{
			SystemFile file = CurrentNode.DataItem as SystemFile;
			((FileViewer)ob).OpenFile (file.Path);
		}


		internal static async Task PopulateOpenWithViewers (CommandArrayInfo info, Project project, string filePath)
		{
			var viewers = (await IdeServices.DisplayBindingService.GetFileViewers (filePath, project)).ToList ();

			//show the default viewer first
			var def = viewers.FirstOrDefault (v => v.CanUseAsDefault) ?? viewers.FirstOrDefault (v => v.IsExternal);
			if (def != null) {
				CommandInfo ci = info.Add (def.Title, def);
				ci.Description = GettextCatalog.GetString ("Open with '{0}'", def.Title);
				if (viewers.Count > 1)
					info.AddSeparator ();
			}

			//then the builtins, followed by externals
			FileViewer prev = null;
			foreach (FileViewer fv in viewers) {
				if (def != null && fv.Equals (def))
					continue;
				if (prev != null && fv.IsExternal != prev.IsExternal)
					info.AddSeparator ();
				CommandInfo ci = info.Add (fv.Title, fv);
				ci.Description = GettextCatalog.GetString ("Open with '{0}'", fv.Title);
				prev = fv;
			}
		}

		[CommandUpdateHandler (ViewCommands.OpenWithList)]
		public void OnOpenWithUpdate (CommandArrayInfo info)
		{
			PopulateOpenWithViewers (info, null, ((SystemFile)CurrentNode.DataItem).Path).Wait();
		}
	}

}

