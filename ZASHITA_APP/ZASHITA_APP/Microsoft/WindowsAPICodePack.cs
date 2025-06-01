

namespace Microsoft
{
    internal class WindowsAPICodePack
    {
        internal class Dialogs
        {
            public static object CommonFileDialogResult { get; internal set; }

            internal class CommonOpenFileDialog
            {
                public bool IsFolderPicker { get; set; }
                public bool Multiselect { get; set; }
                public string Title { get; set; }
                public IEnumerable<object> FileNames { get; internal set; }

                internal object ShowDialog()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}