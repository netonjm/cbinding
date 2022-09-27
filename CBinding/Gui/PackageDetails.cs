//
// PackageDetails.cs
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

using AppKit;
using System;

namespace CBinding
{
    public class PackageDetailsViewController : NSViewController
    {
        List<string> requiresStore = new List<string> ();
        List<string> libPathsStore = new List<string> ();
        List<string> libsStore = new List<string> ();
        List<string> cflagsStore = new List<string> ();

		public PackageDetailsViewController (Package package)
        {
            package.ParsePackage ();

            var stackView = new NSStackView () {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Orientation = NSUserInterfaceLayoutOrientation.Vertical,
                Alignment = NSLayoutAttribute.Leading,
                Distribution = NSStackViewDistribution.Fill
            };
            View = stackView;

			var contentScrollView = new NSScrollView () { TranslatesAutoresizingMaskIntoConstraints = false };
			stackView.AddArrangedSubview (contentScrollView);

			contentScrollView.LeadingAnchor.ConstraintEqualTo (stackView.LeadingAnchor).Active = true;
			contentScrollView.TrailingAnchor.ConstraintEqualTo (stackView.TrailingAnchor).Active = true;

			var contentStackView = new NSStackView () {
				TranslatesAutoresizingMaskIntoConstraints = false,
				Orientation = NSUserInterfaceLayoutOrientation.Vertical,
				Alignment = NSLayoutAttribute.Leading,
				Distribution = NSStackViewDistribution.Fill
			};
			contentScrollView.DocumentView = contentStackView;

			var requiresTreeView = new NSTableView () { TranslatesAutoresizingMaskIntoConstraints = false };
			contentStackView.AddArrangedSubview (requiresTreeView);

			requiresTreeView.HeightAnchor.ConstraintEqualTo (130).Active = true;
			requiresTreeView.LeadingAnchor.ConstraintEqualTo (contentStackView.LeadingAnchor).Active = true;
			requiresTreeView.TrailingAnchor.ConstraintEqualTo (contentStackView.TrailingAnchor).Active = true;

			var libPathsTreeView = new NSTableView () { TranslatesAutoresizingMaskIntoConstraints = false };
			contentStackView.AddArrangedSubview (libPathsTreeView);

			libPathsTreeView.HeightAnchor.ConstraintEqualTo (130).Active = true;
			libPathsTreeView.LeadingAnchor.ConstraintEqualTo (contentStackView.LeadingAnchor).Active = true;
			libPathsTreeView.TrailingAnchor.ConstraintEqualTo (contentStackView.TrailingAnchor).Active = true;

			var libsTreeView = new NSTableView () { TranslatesAutoresizingMaskIntoConstraints = false };
			contentStackView.AddArrangedSubview (libsTreeView);

			libsTreeView.HeightAnchor.ConstraintEqualTo (130).Active = true;
			libsTreeView.LeadingAnchor.ConstraintEqualTo (contentStackView.LeadingAnchor).Active = true;
			libsTreeView.TrailingAnchor.ConstraintEqualTo (contentStackView.TrailingAnchor).Active = true;

			var cflagsTreeView = new NSTableView () { TranslatesAutoresizingMaskIntoConstraints = false };
			contentStackView.AddArrangedSubview (cflagsTreeView);

			cflagsTreeView.HeightAnchor.ConstraintEqualTo (130).Active = true;
			cflagsTreeView.LeadingAnchor.ConstraintEqualTo (contentStackView.LeadingAnchor).Active = true;
			cflagsTreeView.TrailingAnchor.ConstraintEqualTo (contentStackView.TrailingAnchor).Active = true;

			contentStackView.AddArrangedSubview (new NSView () { TranslatesAutoresizingMaskIntoConstraints = false });

			foreach (string req in package.Requires)
                requiresStore.Add (req);

            foreach (string libpath in package.LibPaths)
                libPathsStore.Add (libpath);

            foreach (string lib in package.Libs)
                libsStore.Add (lib);

            foreach (string cflag in package.CFlags)
                cflagsStore.Add (cflag);

            requiresTreeView.ReloadData ();
            libPathsTreeView.ReloadData ();
            libsTreeView.ReloadData ();
			cflagsTreeView.ReloadData ();

			var toolboxStackView = new NSStackView () {
				TranslatesAutoresizingMaskIntoConstraints = false,
				Orientation = NSUserInterfaceLayoutOrientation.Horizontal,
				Alignment = NSLayoutAttribute.CenterX,
				Distribution = NSStackViewDistribution.Fill
			};
			stackView.AddArrangedSubview (toolboxStackView);
			toolboxStackView.LeadingAnchor.ConstraintEqualTo (stackView.LeadingAnchor).Active = true;
			toolboxStackView.TrailingAnchor.ConstraintEqualTo (stackView.TrailingAnchor).Active = true;

			toolboxStackView.AddArrangedSubview (new NSView());

			var okButton = new NSButton () { TranslatesAutoresizingMaskIntoConstraints = false, Title = "Ok" };
			okButton.BezelStyle = NSBezelStyle.RoundRect;
			toolboxStackView.AddArrangedSubview (okButton);
			okButton.WidthAnchor.ConstraintEqualTo (200).Active = true;

			okButton.Activated += OnButtonOkClicked;
		}

		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
        {
            View.Window.Close ();
        }
    }
}
