using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GameTools
{
    /// <summary>
    /// 基于Windows Netsh命令实现的网络限速器
    /// </summary>
    public class NetshNetworkLimiter : INetworkLimiter
    {
        private readonly NetshFirewallManager _firewallManager;
        private string _currentRuleName = string.Empty;
        
        public NetshNetworkLimiter()
        {
            _firewallManager = NetshFirewallManager.Instance;
        }
        
        public async Task<bool> ApplyLimit(int processId, int uploadLimitKBps, int downloadLimitKBps)
        {
            try
            {
                Console.WriteLine($"开始应用网络限速 - 进程ID: {processId}, 上传: {uploadLimitKBps}KB/s, 下载: {downloadLimitKBps}KB/s");
                
                // 检查是否具有管理员权限
                if (!NetshFirewallManager.IsRunningAsAdmin())
                {
                    Console.WriteLine("需要管理员权限才能应用网络限速");
                    throw new UnauthorizedAccessException("需要管理员权限才能应用网络限速");
                }
                
                // 获取进程可执行文件路径
                string executablePath = GetProcessPath(processId);
                if (string.IsNullOrEmpty(executablePath))
                {
                    Console.WriteLine($"无法找到进程ID {processId} 的可执行文件路径");
                    throw new FileNotFoundException($"无法找到进程ID {processId} 的可执行文件路径");
                }
                
                Console.WriteLine($"进程路径: {executablePath}");
                
                // 生成防火墙规则名称
                string processName = Path.GetFileNameWithoutExtension(executablePath);
                _currentRuleName = $"GameTools_NetLimit_{processName}_{processId}";
                Console.WriteLine($"规则名称: {_currentRuleName}");
                
                // 移除现有规则（如果存在）
                await RemoveLimit(processId);
                
                // 针对不同Windows版本和配置尝试不同方法
                bool success = false;
                
                Console.WriteLine("尝试使用QoS策略方法...");
                try
                {
                    // 方法1: QoS策略方法
                    bool uploadSuccess = true;
                    bool downloadSuccess = true;
                    
                    // 上传限速（出站流量）
                    if (uploadLimitKBps > 0)
                    {
                        uploadSuccess = _firewallManager.CreateQoSPolicy(
                            $"{_currentRuleName}_Upload", 
                            executablePath, 
                            uploadLimitKBps, 
                            "out");
                    }
                    
                    // 下载限速（入站流量）
                    if (downloadLimitKBps > 0)
                    {
                        downloadSuccess = _firewallManager.CreateQoSPolicy(
                            $"{_currentRuleName}_Download", 
                            executablePath, 
                            downloadLimitKBps, 
                            "in");
                    }
                    
                    success = uploadSuccess || downloadSuccess;
                    if (success)
                    {
                        Console.WriteLine("QoS策略方法应用成功");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"QoS策略方法失败: {ex.Message}");
                }
                
                // 如果QoS方法失败，尝试其他方法
                if (!success)
                {
                    try
                    {
                        Console.WriteLine("尝试使用接口策略方法...");
                        // 方法2: 使用netsh interface ipv4 命令直接添加策略
                        
                        // 创建入站（下载）限速规则
                        if (downloadLimitKBps > 0)
                        {
                            string downloadArgs = $"interface ipv4 add policy name=\"{_currentRuleName}_Download\" " +
                                $"dir=in throttlelimit={downloadLimitKBps} throttlelimittype=kbps program=\"{executablePath}\"";
                            
                            bool downloadSuccess = await Task.Run(() => _firewallManager.ExecuteNetshCommand(downloadArgs));
                            if (downloadSuccess)
                            {
                                Console.WriteLine("入站限速规则成功应用");
                                success = true;
                            }
                        }
                        
                        // 创建出站（上传）限速规则
                        if (uploadLimitKBps > 0)
                        {
                            string uploadArgs = $"interface ipv4 add policy name=\"{_currentRuleName}_Upload\" " +
                                $"dir=out throttlelimit={uploadLimitKBps} throttlelimittype=kbps program=\"{executablePath}\"";
                            
                            bool uploadSuccess = await Task.Run(() => _firewallManager.ExecuteNetshCommand(uploadArgs));
                            if (uploadSuccess)
                            {
                                Console.WriteLine("出站限速规则成功应用");
                                success = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"接口策略方法失败: {ex.Message}");
                    }
                }
                
                // 尝试应用额外的限速设置
                if (success)
                {
                    await ApplyAdditionalSettings();
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用网络限速失败: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> RemoveLimit(int processId)
        {
            try
            {
                Console.WriteLine($"开始移除网络限速 - 进程ID: {processId}");
                
                // 检查是否具有管理员权限
                if (!NetshFirewallManager.IsRunningAsAdmin())
                {
                    Console.WriteLine("需要管理员权限才能移除网络限速");
                    throw new UnauthorizedAccessException("需要管理员权限才能移除网络限速");
                }
                
                // 如果没有当前规则名称，尝试根据进程ID生成
                if (string.IsNullOrEmpty(_currentRuleName))
                {
                    string executablePath = GetProcessPath(processId);
                    if (!string.IsNullOrEmpty(executablePath))
                    {
                        string processName = Path.GetFileNameWithoutExtension(executablePath);
                        _currentRuleName = $"GameTools_NetLimit_{processName}_{processId}";
                        Console.WriteLine($"为进程 {processId} 生成规则名称: {_currentRuleName}");
                    }
                    else
                    {
                        // 如果无法获取进程路径，尝试使用通用名称
                        _currentRuleName = $"GameTools_NetLimit_Process_{processId}";
                        Console.WriteLine($"无法获取进程路径，使用通用规则名: {_currentRuleName}");
                    }
                }
                
                Console.WriteLine($"尝试清理所有可能的网络限速规则...");
                // 清理QoS规则
                bool qosRulesCleaned = await CleanQoSRules();
                
                // 清理接口策略
                bool interfacePoliciesCleaned = await CleanInterfacePolicies();
                
                // 清理防火墙规则
                bool firewallRulesCleaned = await CleanFirewallRules();
                
                // 恢复全局网络设置
                bool globalSettingsRestored = await RestoreGlobalSettings();
                
                // 清空当前规则名称
                _currentRuleName = string.Empty;
                
                // 任何一步成功都返回true
                bool success = qosRulesCleaned || interfacePoliciesCleaned || firewallRulesCleaned || globalSettingsRestored;
                Console.WriteLine($"网络限速清理" + (success ? "成功" : "失败"));
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"移除网络限速失败: {ex.Message}");
                return false;
            }
        }
        
        // 清理QoS规则
        private async Task<bool> CleanQoSRules()
        {
            try
            {
                Console.WriteLine("正在清理QoS规则...");
                
                // 移除上传和下载的QoS策略
                bool uploadRemoved = await Task.Run(() => _firewallManager.RemoveQoSPolicy($"{_currentRuleName}_Upload"));
                bool downloadRemoved = await Task.Run(() => _firewallManager.RemoveQoSPolicy($"{_currentRuleName}_Download"));
                
                Console.WriteLine($"QoS上传规则清理" + (uploadRemoved ? "成功" : "失败"));
                Console.WriteLine($"QoS下载规则清理" + (downloadRemoved ? "成功" : "失败"));
                
                return uploadRemoved || downloadRemoved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理QoS规则时出错: {ex.Message}");
                return false;
            }
        }
        
        // 清理接口策略
        private async Task<bool> CleanInterfacePolicies()
        {
            try
            {
                Console.WriteLine("正在清理接口策略...");
                
                bool uploadPolicyRemoved = await Task.Run(() => 
                    _firewallManager.ExecuteNetshCommand($"interface ipv4 delete policy name=\"{_currentRuleName}_Upload\""));
                    
                bool downloadPolicyRemoved = await Task.Run(() => 
                    _firewallManager.ExecuteNetshCommand($"interface ipv4 delete policy name=\"{_currentRuleName}_Download\""));
                
                Console.WriteLine($"接口上传策略清理" + (uploadPolicyRemoved ? "成功" : "失败"));
                Console.WriteLine($"接口下载策略清理" + (downloadPolicyRemoved ? "成功" : "失败"));
                
                return uploadPolicyRemoved || downloadPolicyRemoved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理接口策略时出错: {ex.Message}");
                return false;
            }
        }
        
        // 清理防火墙规则
        private async Task<bool> CleanFirewallRules()
        {
            try
            {
                Console.WriteLine("正在清理防火墙规则...");
                
                bool uploadRuleRemoved = await Task.Run(() => 
                    _firewallManager.ExecuteNetshCommand($"advfirewall firewall delete rule name=\"{_currentRuleName}_Upload\""));
                    
                bool downloadRuleRemoved = await Task.Run(() => 
                    _firewallManager.ExecuteNetshCommand($"advfirewall firewall delete rule name=\"{_currentRuleName}_Download\""));
                
                // 尝试删除可能存在的通用规则
                bool genericRuleRemoved = await Task.Run(() => 
                    _firewallManager.ExecuteNetshCommand($"advfirewall firewall delete rule name=\"{_currentRuleName}\""));
                
                Console.WriteLine($"防火墙规则清理" + 
                    ((uploadRuleRemoved || downloadRuleRemoved || genericRuleRemoved) ? "成功" : "失败"));
                
                return uploadRuleRemoved || downloadRuleRemoved || genericRuleRemoved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理防火墙规则时出错: {ex.Message}");
                return false;
            }
        }
        
        // 恢复全局网络设置
        private async Task<bool> RestoreGlobalSettings()
        {
            try
            {
                Console.WriteLine("正在恢复全局网络设置...");
                
                await Task.Run(() => {
                    _firewallManager.ExecuteNetshCommand("interface tcp set global autotuninglevel=normal");
                    _firewallManager.ExecuteNetshCommand("interface tcp set global congestionprovider=default");
                    _firewallManager.ExecuteNetshCommand("interface tcp set global ecncapability=default");
                });
                
                Console.WriteLine("全局网络设置已恢复");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"恢复全局网络设置时出错: {ex.Message}");
                return false;
            }
        }
        
        private string GetProcessPath(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                
                // 尝试使用MainModule获取路径
                try
                {
                    return process.MainModule?.FileName ?? string.Empty;
                }
                catch
                {
                    // 如果无法访问MainModule，使用WMI获取
                    using (var searcher = new System.Management.ManagementObjectSearcher(
                        $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {processId}"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            return obj["ExecutablePath"]?.ToString() ?? string.Empty;
                        }
                    }
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        
        // 应用额外的限速设置
        private async Task ApplyAdditionalSettings()
        {
            try
            {
                Console.WriteLine("正在应用额外的网络设置...");
                
                // 设置TCP全局参数，优化网络流量控制
                await Task.Run(() => {
                    _firewallManager.ExecuteNetshCommand("interface tcp set global ecncapability=enabled");
                    _firewallManager.ExecuteNetshCommand("interface tcp set global congestionprovider=ctcp");
                });
                
                Console.WriteLine("额外设置应用完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用额外设置时出错: {ex.Message}");
            }
        }
        
        // 添加实现 INetworkLimiter.SetLimit 方法
        public async Task<bool> SetLimit(int processId, string processName, int uploadLimitKBps, int downloadLimitKBps)
        {
            return await ApplyLimit(processId, uploadLimitKBps, downloadLimitKBps);
        }
    }
} 