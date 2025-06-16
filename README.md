# Avalonia.FileDroper
## 概述
解决 Avalonia 在Linux 中文件拖拽失效及Win10 UAC 下使用管理员权限导致的文件拖拽失效问题

## axaml文件

MainWindow.axaml
```bash
  <Window.Child>
    <views:MainView/>
  </Window.Child>
</Window>
```
## cs文件
MainWindow.axaml.cs
```bash
public partial class MainWindow : DragDropWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```
