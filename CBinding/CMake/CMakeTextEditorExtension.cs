//
// CMakeTextEditorExtension.cs
//
// Author:
//       Elsayed Awdallah <comando4ever@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Documents;

namespace CBinding
{
	//TODO: FIX
    class CTextViewController : DocumentController
    {
        public bool Editable { get; internal set; }
        public IReadOnlyRegion ReadOnlyRegion { get; internal set; }
    }

    [Name (nameof (ImmediatePadKeyboardInputCommandHandler))]
	[ContentType ("text")]
	[TextViewRole (nameof (CTextViewController))]
	[Export (typeof (ICommandHandler))]
	[Order (Before = PredefinedCompletionNames.CompletionCommandHandler)]
	sealed class ImmediatePadKeyboardInputCommandHandler :
	   ICommandHandler<TypeCharCommandArgs>,
	   ICommandHandler<BackspaceKeyCommandArgs>
	{
		public string DisplayName => nameof (ImmediatePadKeyboardInputCommandHandler);

		CTextViewController GetViewController (ICocoaTextView cocoaTextView)
		{
			return (CTextViewController)cocoaTextView.Properties [typeof (CTextViewController)];
		}

		SnapshotPoint GetReadOnlyEndPoint (ICocoaTextView cocoaTextView, CTextViewController controller)
		{
			var textBuffer = cocoaTextView.TextBuffer;
			var snapshot = textBuffer.CurrentSnapshot;
			var span = controller.ReadOnlyRegion.Span;

			return span.GetEndPoint (snapshot);
		}

		#region TypedChar

		public CommandState GetCommandState (TypeCharCommandArgs args)
		{
			return CommandState.Available;
		}

		public bool ExecuteCommand (TypeCharCommandArgs args, CommandExecutionContext executionContext)
		{
			var cocoaTextView = (ICocoaTextView)args.TextView;
			var controller = GetViewController (cocoaTextView);

			if (!controller.Editable)
				return true;

			if (controller.ReadOnlyRegion == null)
				return false;

			var caret = cocoaTextView.Caret.Position.BufferPosition;
			var end = GetReadOnlyEndPoint (cocoaTextView, controller);

			// Note: when the user has their caret located anywhere inside of the ReadOnlyRegion,
			// we want to make sure to jump the caret to the current "input line" which starts
			// 1 ("\n") character beyond the end of the ReadOnlyRegion.
			if (caret.Position <= end.Position)
				cocoaTextView.Caret.MoveTo (end.Add (1));

			return false;
		}

		#endregion TypedChar

		#region BackspaceKey

		public CommandState GetCommandState (BackspaceKeyCommandArgs args)
		{
			return CommandState.Available;
		}

		public bool ExecuteCommand (BackspaceKeyCommandArgs args, CommandExecutionContext executionContext)
		{
			var cocoaTextView = (ICocoaTextView)args.TextView;
			var controller = GetViewController (cocoaTextView);

			if (!controller.Editable)
				return true;

			if (controller.ReadOnlyRegion == null)
				return false;

			var caret = cocoaTextView.Caret.Position.BufferPosition;
			var end = GetReadOnlyEndPoint (cocoaTextView, controller);

			// Note: the last "\n" at the end of the ReadOnlyRegion is not protected, so
			// this logic is to make sure the user does not backspace over it.
			if (caret.Position == end.Position + 1)
				return true;

			return false;
		}

		#endregion BackspaceKey
	}

	//class CMakeTextEditorExtension : CompletionTextEditorExtension
	//{
	//	bool IsComment (CodeCompletionContext context)
	//	{
	//		string text = Editor.GetTextBetween (0, Editor.CaretOffset);

	//		if (text.Length == 0)
	//			return false;

	//		if (text.LastIndexOf ('#') > text.LastIndexOf ('\n'))
	//			return true;

	//		if (text.LastIndexOf ("#[[", System.StringComparison.Ordinal) >
	//			text.LastIndexOf ("]]", System.StringComparison.Ordinal))
	//			return true;

	//		return false;
	//	}

	//	bool IsCommand (CodeCompletionContext context)
	//	{
	//		string text = Editor.GetTextBetween (0, Editor.CaretOffset);

	//		text = Regex.Replace (text, @"#\[\[.*\]\]", "", RegexOptions.Singleline);
	//		text = Regex.Replace (text, @"#.*\n", "\n", RegexOptions.Singleline);

	//		int openParents = text.Count ((char arg) => arg == '(');
	//		int closeParents = text.Count ((char arg) => arg == ')');

	//		if (openParents <= closeParents)
	//			return true;

	//		return false;
	//	}

	//	bool IsString (CodeCompletionContext context)
	//	{
	//		string text = Editor.GetTextBetween (0, Editor.CaretOffset);

	//		text = Regex.Replace (text, @"#\[\[.*\]\]", "", RegexOptions.Singleline);
	//		text = Regex.Replace (text, @"#.*\n", "\n", RegexOptions.Singleline);

	//		int quotes = text.Count ((char arg) => arg == '"');

	//		MatchCollection openString = Regex.Matches (text, @"\[[0-9]*=\[");
	//		MatchCollection closeString = Regex.Matches (text, @"\][0-9]*=\]");
	//		if (openString.Count > 0) {
	//			if (closeString.Count <= 0)
	//				return true;

	//			if (openString [openString.Count].Index > closeString [closeString.Count].Index)
	//				return true;
	//		}

	//		if (quotes % 2 == 0)
	//			return false;

	//		return true;
	//	}

	//	public override Task<ICompletionDataList> HandleCodeCompletionAsync (CodeCompletionContext completionContext,
	//																		 char completionChar,
	//																		 CancellationToken token = default (CancellationToken))
	//	{
	//		if (!char.IsLetter (completionChar))
	//			return null;

	//		if (completionContext.TriggerOffset > 1 &&
	//			(char.IsLetterOrDigit (Editor.GetCharAt (completionContext.TriggerOffset - 2))
	//			|| Editor.GetCharAt (completionContext.TriggerOffset - 2) == '_'))
	//			return null;

	//		if (IsComment (completionContext) || IsString (completionContext))
	//			return null;

	//		if (IsCommand (completionContext)) {
	//			var list = new CompletionDataList (CMakeCompletionDataLists.Commands);
	//			list.TriggerWordLength = 1;

	//			return Task.FromResult<ICompletionDataList> (list);
	//		}
	//		return null;
	//	}
	//}
}
