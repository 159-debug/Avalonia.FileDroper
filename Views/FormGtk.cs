using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Gtk;
using GTKSystem.Windows.Forms.GTKControls.ControlBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DragDropEffects = Avalonia.Input.DragDropEffects;
using DragEventArgs = Avalonia.Input.DragEventArgs;

namespace Avalonia.FileDroper.Views
{
    public partial class FormGtk : Form
    {
        private const string libX11 = "libX11.so.6";

        [DllImport(libX11)]
        private static extern nint XOpenDisplay(nint display);

        [DllImport(libX11)]
        private static extern nint XCreateSimpleWindow(nint display, nint parent, int x = 0, int y = 0, uint width = 1, uint height = 1, uint border_width = 0, nint border = 0, nint background = 0);

        [DllImport(libX11)]
        private static extern void XReparentWindow(nint display, nint window, nint parent, int x, int y);

        [DllImport(libX11)]
        private static extern void XFlush(nint display);

        public FormGtk()
        {
            InitializeComponent();
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Opacity = 0.01;
            this.StartPosition = FormStartPosition.Manual;
            TargetEntry[] targets = new TargetEntry[]
            {
                new TargetEntry("text/uri-list", 0, 0)
            };
            Drag.SourceSet((FormBase)this.GtkControl, 0, targets, Gdk.DragAction.Copy);
            Drag.DestSet((FormBase)this.GtkControl, 0, targets, 0);
            ((FormBase)this.GtkControl).DragMotion += Form1_DragMotion;
            ((FormBase)this.GtkControl).DragDrop += Form1_DragDrop;
            ((FormBase)this.GtkControl).DragDataReceived += Form1_DragDataReceived;
        }

        public Gtk.Window Window
        {
            get
            {
                return ((FormBase)this.GtkControl);
            }
        }

        public event EventHandler<DragEventArgs> DragDataReceived;

        private void Form1_DragDataReceived(object o, DragDataReceivedArgs args)
        {
            args.RetVal = true;
            List<string> flies = new List<string>();
            if (args.SelectionData.Length > 0 && args.SelectionData.Format == 8)
            {
                byte[] data = args.SelectionData.Data;
                string encoded = System.Text.Encoding.UTF8.GetString(data);
                var paths = new List<string>(encoded.Split('\r', '\n'));
                string[] uris = System.Text.Encoding.UTF8.GetString(args.SelectionData.Data).Split('\r', '\n');
                foreach (var uri in uris)
                {
                    if (uri.StartsWith("file:///"))
                    {
                        string decodedUrl = WebUtility.UrlDecode(uri.Substring(7));
                        Console.WriteLine(decodedUrl);
                        if (File.Exists(decodedUrl))
                            flies.Add(decodedUrl);
                    }
                }
                if (flies.Count > 0)
                {
                    Dispatcher.UIThread.Invoke(new System.Action(() =>
                    {
                        DragDataReceived?.Invoke(this, new DragEventArgs(Avalonia.Input.DragDrop.DropEvent, new GtkFileDataObject(flies.ToArray()) { Point = new Avalonia.Point(args.X, args.Y) },
                       new Avalonia.Interactivity.Interactive(), new Avalonia.Point(args.X, args.Y), KeyModifiers.None)
                        { DragEffects = DragDropEffects.Move });
                    }));
                }
            }
        }

        private void Form1_DragDrop(object o, DragDropArgs args)
        {
            args.RetVal = true;
            if (args.Context.ListTargets().Length > 0)
            {
                foreach (var target in args.Context.ListTargets())
                {
                    if (target.Name == "text/uri-list" || target.Name == "text/x-moz-url")
                    {
                        // 处理 URI 列表
                        Drag.GetData((Widget)o, args.Context, target, args.Time);
                    }
                    else if (target.Name == "text/plain" || target.Name == "STRING" || target.Name == "UTF8_STRING")
                    {
                        // 处理普通文本
                        Drag.GetData((Widget)o, args.Context, target, args.Time);
                    }
                }
            }
        }

        private void Form1_DragMotion(object o, DragMotionArgs args)
        {
            Gdk.Drag.Status(args.Context, args.Context.SuggestedAction, args.Time);
            args.RetVal = true;
        }
    }
}
