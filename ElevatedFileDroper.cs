using Avalonia.Controls;
namespace Avalonia.FileDroper
{
    public partial class ElevatedFileDroper
    {
        public event EventHandler DragDrop;
        public string[] DropFilePaths { get; private set; }
        public POINT DropPoint { get; private set; }
        public TopLevel TopLevel;

        public void AddHook(TopLevel topLevel)
        {
            TopLevel = topLevel;
            Avalonia.Controls.Win32Properties.AddWndProcHookCallback(topLevel, WndProc);
            IntPtr handle = topLevel.TryGetPlatformHandle().Handle; //this.HwndSource.Handle;
            if (IsUserAnAdmin()) RevokeDragDrop(handle);
            DragAcceptFiles(handle, true);
            ChangeMessageFilter(handle);
        }

        public void Refresh()
        {
            IntPtr handle = TopLevel.TryGetPlatformHandle().Handle;
            DragAcceptFiles(handle, false);
            if (IsUserAnAdmin()) RevokeDragDrop(handle);
            DragAcceptFiles(handle, true);
            ChangeMessageFilter(handle);
        }

        public void RemoveHook()
        {
            Avalonia.Controls.Win32Properties.RemoveWndProcHookCallback(TopLevel, WndProc);
            DragAcceptFiles(TopLevel.TryGetPlatformHandle().Handle, false);
        }


        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (TryGetDropInfo((int)msg, wParam, out string[] filePaths, out POINT point))
            {
                DropPoint = point;
                DropFilePaths = filePaths;
                DragDrop?.Invoke(this, null);
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
