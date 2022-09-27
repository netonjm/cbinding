using System;
using ClangSharp;
using MonoDevelop.Core;

namespace CBinding.Parser
{
	/// <summary>
	/// Represents clang abstract symbols from the built AST
	/// </summary>
	public class Symbol
	{
		public bool IsDefinition { get; internal set; }
		public string Name { get; internal set; }
		public CX_CXXAccessSpecifier Access { get; internal set; }
		public SourceLocation Begin { get; }

		public CXCursor Represented { get; }

		public string Usr { get; }

		public bool Def { get; }
        public FilePath FileName { get; internal set; }

        public Symbol (CProject project, CXCursor cursor)
		{
			lock (project.ClangManager.SyncRoot) {
				Represented = cursor;
				Usr = clang.getCursorUSR (cursor).ToString ();
				Begin = project.ClangManager.GetSourceLocation (
					clang.getRangeStart (clang.Cursor_getSpellingNameRange (cursor, 0, 0))
				);
				Def = clang.isCursorDefinition (cursor) != 0;
			}
		}
	}
}