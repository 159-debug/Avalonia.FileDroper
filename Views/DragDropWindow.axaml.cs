using Avalonia.Controls;
using Avalonia.FileDroper.Views;
using Avalonia.Input;
using Avalonia.Metadata;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Avalonia.FileDroper;

public partial class DragDropWindow : Window
{
    public DragDropWindow()
    {
        InitializeComponent();
        if (!Design.IsDesignMode)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var gtkView = new GtkView();
                gtkView.DragDataReceived += GtkView_DragDataReceived;
                gtkControl.Content = gtkView;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= -1 && IsUACEnabled())
            {
                var droper = new ElevatedFileDroper();
                this.Loaded += (sender, e) =>
                {
                    droper.AddHook(this);
                };
                this.PropertyChanged += (sender, e) =>
                {
                    if (e.Property == Window.WindowStateProperty)
                    {
                        if ((WindowState)e.OldValue == WindowState.Minimized)
                        {
                            droper.Refresh();
                        }
                    }
                };
                this.Closing += (sender, e) =>
                {
                    droper.RemoveHook();
                };
                droper.DragDrop += (sender, e) =>
                {
                    if (sender is global::Avalonia.FileDroper.ElevatedFileDroper fileDroper)
                    {
                        var inputElement = this.InputHitTest(new Point(fileDroper.DropPoint.X, fileDroper.DropPoint.Y));
                        if (inputElement != null)
                            inputElement.RaiseEvent(new DragEventArgs(Avalonia.Input.DragDrop.DropEvent, new GtkFileDataObject(fileDroper.DropFilePaths) { Point = new Avalonia.Point(fileDroper.DropPoint.X, fileDroper.DropPoint.Y) },
                            new Avalonia.Interactivity.Interactive(), new Avalonia.Point(fileDroper.DropPoint.X, fileDroper.DropPoint.Y), KeyModifiers.None)
                            { DragEffects = DragDropEffects.Move });
                    }
                };
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public static bool IsUACEnabled()
    {
        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
            {
                return (int)key?.GetValue("EnableLUA", 1) == 1;
            }
        }
        catch
        {
            return true; // Ä¬ÈÏ·µ»ØÆôÓÃ×´Ì¬
        }
    }


    private void GtkView_DragDataReceived(object? sender, DragEventArgs e)
    {
        if (e.Data is GtkFileDataObject dataObject)
        {
            var inputElement = this.InputHitTest(dataObject.Point);
            if (inputElement != null)
                inputElement.RaiseEvent(e);
        }
    }

    public object? Child
    {
        get { return contentControl.Content; }
        set { contentControl.Content = value; }
    }

}