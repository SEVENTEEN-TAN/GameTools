using System.Configuration;
using System.Data;
using System.Windows;

namespace GameTools;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 检查管理员权限
        if (!NetshFirewallManager.IsRunningAsAdmin())
        {
            System.Windows.MessageBox.Show("该程序需要管理员权限才能操作防火墙规则。请以管理员身份重新启动程序。", 
                           "需要管理员权限", 
                           MessageBoxButton.OK, 
                           MessageBoxImage.Warning);
                           
            Shutdown();
            return;
        }
    }
}

