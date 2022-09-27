//
// ProjectPackagesFolderNodeBuilder.cs: Node to control the packages in the project
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

using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;

namespace CBinding.ProjectPad
{	
	public class ProjectPackagesFolderNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ProjectPackageCollection); }
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			CProject project = ((ProjectPackageCollection)dataObject).Project;
			if (project == null) return;
			project.PackageAddedToProject += OnAddPackage;
			project.PackageRemovedFromProject += OnRemovePackage;
		}

		public override void OnNodeRemoved (object dataObject)
		{
			CProject project = ((ProjectPackageCollection)dataObject).Project;
			if (project == null) return;
			project.PackageAddedToProject -= OnAddPackage;
			project.PackageRemovedFromProject -= OnRemovePackage;
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectPackagesFolderNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "Packages";
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{			
			nodeInfo.Label = "Packages";
			nodeInfo.Icon = Context.GetIcon (Stock.OpenReferenceFolder);
			nodeInfo.ClosedIcon = Context.GetIcon (Stock.ClosedReferenceFolder);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ((ProjectPackageCollection)dataObject).Count > 0;
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			ProjectPackageCollection packages = (ProjectPackageCollection)dataObject;
			
			foreach (Package p in packages)
				treeBuilder.AddChild (p);
		}
		
		public override string ContextMenuAddinPath {
			get { return "/CBinding/Views/ProjectBrowser/ContextMenu/PackagesFolderNode"; }
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			return -1;
		}
		
		private void OnAddPackage (object sender, ProjectPackageEventArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (e.Project.Packages);
			if (builder != null)
				builder.UpdateAll ();
		}
		
		private void OnRemovePackage (object sender, ProjectPackageEventArgs e)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (e.Project.Packages);
			if (builder != null)
				builder.UpdateAll ();
		}
	}
	
	public class ProjectPackagesFolderNodeCommandHandler : NodeCommandHandler
	{
		[CommandHandler (CBinding.CProjectCommands.AddPackage)]
		public void AddPackageToProject ()
		{
			CProject project = (CProject)CurrentNode.GetParentDataItem (
			    typeof(CProject), false);

			var viewController = new EditPackagesDialogViewController (project);
			var window = NSWindow.GetWindowWithContentViewController (viewController);
			window.Title = "Edit Packages";

			var size = new CGSize (600, 400);
			window.MinSize = size;
			window.SetContentSize (size);

			MessageService.ShowCustomDialog (window);
			
			IdeApp.ProjectOperations.SaveAsync (project);
			CurrentNode.Expanded = true;
		}
		
		// Currently only accepts packages and projects that compile into a static library
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			if (dataObject is Package)
				return true;
			
			if (dataObject is CProject) {
				CProject project = (CProject)dataObject;
				
				if (((ProjectPackageCollection)CurrentNode.DataItem).Project.Equals (project))
					return false;
				
				CProjectConfiguration config = (CProjectConfiguration)project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
				
				if (config.CompileTarget != CompileTarget.Exe)
					return true;
			}
			
			return false;
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy | DragOperation.Move;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			List<IWorkspaceFileObject> toSave = new List<IWorkspaceFileObject> ();
			if (dataObject is Package) {
				Package package = (Package)dataObject;
				ITreeNavigator nav = CurrentNode;
				
				CProject dest = nav.GetParentDataItem (typeof(CProject), true) as CProject;
				nav.MoveToObject (dataObject);
				CProject source = nav.GetParentDataItem (typeof(CProject), true) as CProject;
				
				dest.Packages.Add (package);
				toSave.Add (dest);

				if (operation == DragOperation.Move) {
					source.Packages.Remove (package);
					toSave.Add (source);
				}
			} else if (dataObject is CProject) {
				CProject draggedProject = (CProject)dataObject;
				CProject destProject = (CurrentNode.DataItem as ProjectPackageCollection).Project;
				
				draggedProject.WriteMDPkgPackage (IdeApp.Workspace.ActiveConfiguration);
				
				Package package = new Package (draggedProject);
				
				if (!destProject.Packages.Contains (package)) {				
					destProject.Packages.Add (package);
					toSave.Add (destProject);
				}
			}
			IdeApp.ProjectOperations.SaveAsync (toSave);
		}
	}
}
