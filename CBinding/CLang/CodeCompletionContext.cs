
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Projects;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Adornments;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Text.Operations;
using System.Runtime.InteropServices;
using System.Text;

namespace CBinding
{
    //[Export (typeof (ITextViewConnectionListener))]
    //[TextViewRole (PredefinedTextViewRoles.Document)]
    //[ContentType (CContentTypeDefinition.cDelegationContentType)]
    //internal class CTextViewConnectionListener : ICocoaTextViewConnectionListener
    //{
    //    private readonly IEditorOptionsFactoryService _editorOptionsFactory;
    //    private readonly List<ICocoaTextView> _trackedViews;

    //    [ImportingConstructor]
    //    public CTextViewConnectionListener (IEditorOptionsFactoryService editorOptionsFactory)
    //    {
    //        _editorOptionsFactory = editorOptionsFactory;

    //        _trackedViews = new ();

    //        //HtmlSettings.Changed += OnSettingsChanged;
    //    }


    //    private void SetViewOptions (ITextView textView)
    //    {
    //        IEditorOptions options = _editorOptionsFactory.GetOptions (textView);

    //        //options.SetOptionValue (DefaultOptions.ConvertTabsToSpacesOptionId, HtmlSettings.IndentType == IndentType.Spaces);
    //        //options.SetOptionValue (DefaultOptions.IndentSizeOptionId, HtmlSettings.IndentSize);
    //        //options.SetOptionValue (DefaultOptions.TabSizeOptionId, HtmlSettings.TabSize);
    //    }

    //    void ICocoaTextViewConnectionListener.SubjectBuffersConnected (ICocoaTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
    //    {
    //        if (subjectBuffers.Any (b => b.ContentType.IsOfType (CContentTypeDefinition.cDelegationContentType))) {
    //            SetViewOptions (textView);

    //            _trackedViews.Add (textView);
    //        }
    //    }

    //    void ICocoaTextViewConnectionListener.SubjectBuffersDisconnected (ICocoaTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
    //    {
    //        if (subjectBuffers.Any (b => b.ContentType.IsOfType (CContentTypeDefinition.cDelegationContentType))) {
    //            _trackedViews.Remove (textView);
    //        }
    //    }
    //}

    class VSCEditorDocument
    {
        private CEditorDocument document;

        public VSCEditorDocument (CEditorDocument document)
        {
            this.document = document;
        }
    }

    class CEditorDocument
    {
        internal CEditorDocument (
         ITextBuffer textBuffer,
         bool disableContainedLanguages)
        {
            ServiceManager.AddService (this, TextBuffer);
        }

        public event EventHandler<EventArgs>? Activated;
        public event EventHandler<EventArgs>? Deactivated;


        public IPropertyOwner TextBuffer { get; internal set; }
    }

    public class CodeCompletionContext
    {
        public uint TriggerLine { get; internal set; }
        public int TriggerLineOffset { get; internal set; }
    }
}