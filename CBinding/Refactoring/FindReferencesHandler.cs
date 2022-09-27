﻿using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using ClangSharp;
using System.Collections.Generic;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.Gui;
using CBinding.Parser;

namespace CBinding.Refactoring
{	
	//Based on code from CSharpBinding
	/// <summary>
	/// Find references handler.
	/// </summary>
	public class FindReferencesHandler
	{
		CProject project;
		CXCursor cursorReferenced;
		string UsrReferenced;
		public string File;

		/// <summary>
		/// Initializes a new instance of the <see cref="CBinding.Refactoring.FindReferencesHandler"/> class.
		/// Gets the cursor at the caret's position.
		/// </summary>
		/// <param name="proj">Proj.</param>
		/// <param name="doc">Document.</param>
		public FindReferencesHandler (CProject proj, Document doc)
		{
			project = proj;
			if (!proj.HasLibClang)
				return;

			cursorReferenced = project.ClangManager.GetCursorReferenced (
                project.ClangManager.GetCursor (
                    doc.FileName,
					doc.GetTextView ().GetCaretLocation()
				)
            );
            UsrReferenced = project.ClangManager.GetCursorUsrString (cursorReferenced);
		}

		List<Reference> references = new List<Reference>();

		/// <summary>
		/// Visit the specified cursor, parent and data.
		/// </summary>
		/// <param name="cursor">Cursor.</param>
		/// <param name="parent">Parent.</param>
		/// <param name="data">Data.</param>
		public CXChildVisitResult Visit(CXCursor cursor, CXCursor parent, IntPtr data)
		{
			if (!File.Equals (TranslationUnitParser.GetFileName (cursor)))
				return CXChildVisitResult.Continue;
			
			CXCursor referenced = project.ClangManager.GetCursorReferenced (cursor);
			string Usr = project.ClangManager.GetCursorUsrString (referenced);

            if (UsrReferenced.Equals (Usr)) {
                CXSourceRange range = clang.Cursor_getSpellingNameRange (cursor, 0, 0);
                var reference = new Reference (project, cursor, range);

                //FIXME: don't block!
                Document doc = IdeApp.Workbench.OpenDocument (reference.FileName, project, false).Result;

				var editor = doc.GetTextView ();


                //if (!references.Contains (reference)
                //    //this check is needed because explicit namespace qualifiers, eg: "std" from std::toupper
                //    //are also found when finding eg:toupper references, but has the same cursorkind as eg:"toupper"
                //    && doc.Editor.GetTextAt (reference.Begin.Offset, reference.Length).Equals (referenced.ToString ())) {
                //    references.Add (reference);
                //}

            }
            return CXChildVisitResult.Recurse;
		}

		/// <summary>
		/// Finds the references and reports them to the IDE.
		/// </summary>
		/// <param name="project">Project.</param>
		public void FindRefs (CProject project)
		{
            var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
            try {
                lock (project.ClangManager.SyncRoot)
                    project.ClangManager.FindReferences (this);
                foreach (var reference in references) {

					var sr = SearchResult.Create (reference.FileName, reference.Offset,
						reference.Length);
					//var sr = new SearchResult (
     //                   new FileProvider (),
     //               );
                    monitor.ReportResult (sr);
                }
            } catch (Exception ex) {
                if (monitor != null)
                    monitor.ReportError ("Error finding references", ex);
                else
                    LoggingService.LogError ("Error finding references", ex);
            } finally {
                if (monitor != null)
                    monitor.Dispose ();
            }
        }

		/// <summary>
		/// Update the specified info.
		/// </summary>
		/// <param name="info">Info.</param>
		public void Update (CommandInfo info)
		{
			info.Enabled = info.Visible =
				project.HasLibClang &&
				clang.Cursor_isNull (cursorReferenced) == 0 &&
				IsReferenceOrDeclaration (cursorReferenced);
		}

		/// <summary>
		/// Run this instance.
		/// </summary>
		public void Run ()
		{
			FindRefs (project);
		}

		/// <summary>
		/// Determines whether the specified cursor is a declaration or reference by its kind.
		/// </summary>
		/// <returns><c>true</c> if cursor is reference or declaration; otherwise, <c>false</c>.</returns>
		/// <param name="cursor">Cursor.</param>
		bool IsReferenceOrDeclaration(CXCursor cursor)
		{
			return clang.isReference (cursor.kind) != 0 || clang.isDeclaration (cursor.kind) != 0;
		}
	}
}

