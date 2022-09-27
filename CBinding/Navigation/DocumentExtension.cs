//
// FunctionNodeBuilder.cs
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

using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Projects;

namespace CBinding
{
    public static class DocumentExtension
	{
		public static ITextView GetTextView(this Document document)
        {
			return document.GetContent<ITextView> ();
		}

		public static (int x, int y) GetCaretLocation (this ITextView textView)
		{
			return ((int)textView.Caret.Top, (int)textView.Caret.Left);
		}

		public static DocumentController GetDocumentController (this Document textView)
		{
			return textView.GetContent<DocumentController> ();
		}

		public static string GetText (this ITextView textView)
		{
			return textView.TextSnapshot.GetText();
		}
	}
}
