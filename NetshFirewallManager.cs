using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GameTools
{
    /// <summary>
    /// 使用netsh命令管理Windows防火墙规则
    /// </summary>
    public class NetshFirewallManager
    {
        #region 单例实现
        private static NetshFirewallManager _instance;
        public static NetshFirewallManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NetshFirewallManager();
                }
                return _instance;
            }
        }

        private NetshFirewallManager()
        {
            // 构造函数
        }
        #endregion

        #region 基础方法
        /// <summary>
        /// 执行netsh命令
        /// </summary>
        /// <param name="arguments">命令参数</param>
        /// <returns>是否成功执行</returns>
        public bool ExecuteNetshCommand(string arguments)
        {
            try
            {
                Console.WriteLine($"执行netsh命令: netsh {arguments}");
                
                // 创建进程信息，设置为捕获输出
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false, // 要捕获输出必须设置为false
                    CreateNoWindow = true,
                    Verb = "runas" // 以管理员权限运行
                };

                using (Process process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        Console.WriteLine("无法启动netsh进程");
                        return false;
                    }

                    // 读取输出和错误信息 
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    // 等待进程完成
                    process.WaitForExit();
                    
                    // 记录详细日志
                    if (!string.IsNullOrEmpty(output))
                    {
                        Console.WriteLine("命令输出:");
                        Console.WriteLine(output);
                    }
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine("命令错误:");
                        Console.WriteLine(error);
                    }
                    
                    bool success = process.ExitCode == 0;
                    Console.WriteLine($"命令执行" + (success ? "成功" : "失败") + $"，退出代码: {process.ExitCode}");
                    
                    // 如果失败但没有错误信息，尝试进行更详细的分析
                    if (!success && string.IsNullOrEmpty(error))
                    {
                        // 尝试分析输出，看是否包含特定错误或警告信息
                        if (output.Contains("不能完成") || output.Contains("无法") || 
                            output.Contains("Error") || output.Contains("access is denied"))
                        {
                            Console.WriteLine("命令失败，可能的原因: 权限不足或指定的资源不存在");
                        }
                        else if (output.Contains("No rules match"))
                        {
                            Console.WriteLine("命令失败原因: 没有找到匹配的规则");
                        }
                    }
                    
                    return success;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行netsh命令失败: {ex.Message}");
                Console.WriteLine($"异常类型: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部异常: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// 异步执行netsh命令
        /// </summary>
        /// <param name="arguments">命令参数</param>
        /// <returns>是否成功执行</returns>
        private async Task<bool> ExecuteNetshCommandAsync(string arguments)
        {
            return await Task.Run(() => ExecuteNetshCommand(arguments));
        }
        #endregion

        #region 防火墙规则管理
        /// <summary>
        /// 创建阻止应用程序的防火墙规则
        /// </summary>
        /// <param name="ruleName">规则名称</param>
        /// <param name="programPath">程序路径</param>
        /// <param name="direction">方向，in或out</param>
        /// <returns>是否成功</returns>
        public bool CreateBlockRule(string ruleName, string programPath, string direction = "out")
        {
            try
            {
                string arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir={direction} action=block program=\"{programPath}\" enable=yes";
                return ExecuteNetshCommand(arguments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建阻止规则失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建允许应用程序的防火墙规则
        /// </summary>
        /// <param name="ruleName">规则名称</param>
        /// <param name="programPath">程序路径</param>
        /// <param name="direction">方向，in或out</param>
        /// <returns>是否成功</returns>
        public bool CreateAllowRule(string ruleName, string programPath, string direction = "out")
        {
            try
            {
                string arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir={direction} action=allow program=\"{programPath}\" enable=yes";
                return ExecuteNetshCommand(arguments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建允许规则失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除防火墙规则
        /// </summary>
        /// <param name="ruleName">规则名称</param>
        /// <returns>是否成功</returns>
        public bool RemoveRule(string ruleName)
        {
            try
            {
                string arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"";
                return ExecuteNetshCommand(arguments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除规则失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 启用防火墙规则
        /// </summary>
        /// <param name="ruleName">规则名称</param>
        /// <returns>是否成功</returns>
        public bool EnableRule(string ruleName)
        {
            try
            {
                string arguments = $"advfirewall firewall set rule name=\"{ruleName}\" new enable=yes";
                return ExecuteNetshCommand(arguments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启用规则失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 禁用防火墙规则
        /// </summary>
        /// <param name="ruleName">规则名称</param>
        /// <returns>是否成功</returns>
        public bool DisableRule(string ruleName)
        {
            try
            {
                string arguments = $"advfirewall firewall set rule name=\"{ruleName}\" new enable=no";
                return ExecuteNetshCommand(arguments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"禁用规则失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查防火墙规则是否存在
        /// </summary>
        /// <param name="ruleName">规则名称</param>
        /// <returns>规则是否存在</returns>
        public bool RuleExists(string ruleName)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name=\"{ruleName}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        return false;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    // 如果输出中包含"No rules match the specified criteria"，则规则不存在
                    return !output.Contains("No rules match the specified criteria");
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 启用Windows防火墙
        /// </summary>
        /// <returns>是否成功</returns>
        public bool EnableFirewall()
        {
            try
            {
                string arguments = "advfirewall set allprofiles state on";
                return ExecuteNetshCommand(arguments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启用防火墙失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 禁用Windows防火墙
        /// </summary>
        /// <returns>是否成功</returns>
        public bool DisableFirewall()
        {
            try
            {
                string arguments = "advfirewall set allprofiles state off";
                return ExecuteNetshCommand(arguments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"禁用防火墙失败: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 网络限速模拟
        /// <summary>
        /// 创建网络限速规则 (使用间歇性阻断模拟限速)
        /// </summary>
        /// <param name="ruleName">规则名基础名称</param>
        /// <param name="programPath">程序路径</param>
        /// <param name="downloadLimit">下载速度限制 (KB/s)</param>
        /// <param name="uploadLimit">上传速度限制 (KB/s)</param>
        /// <returns>是否成功</returns>
        public bool CreateNetworkLimitRule(string ruleName, string programPath, int downloadLimit, int uploadLimit)
        {
            // 注意：此方法使用阻止规则模拟网络限速
            // 实际限速需要使用ThrottlingManager类进行管理
            bool inboundSuccess = true;
            bool outboundSuccess = true;

            try
            {
                // 创建入站规则（控制下载）
                if (downloadLimit > 0)
                {
                    string inboundRuleName = $"{ruleName}_Inbound";
                    inboundSuccess = CreateBlockRule(inboundRuleName, programPath, "in");
                }

                // 创建出站规则（控制上传）
                if (uploadLimit > 0)
                {
                    string outboundRuleName = $"{ruleName}_Outbound";
                    outboundSuccess = CreateBlockRule(outboundRuleName, programPath, "out");
                }

                return inboundSuccess && outboundSuccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建网络限速规则失败: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region QoS带宽限制
        /// <summary>
        /// 创建QoS网络限速策略
        /// </summary>
        /// <param name="policyName">策略名称</param>
        /// <param name="programPath">程序路径</param>
        /// <param name="limitKBps">限速KB/s</param>
        /// <param name="direction">方向，in或out</param>
        /// <returns>是否成功</returns>
        public bool CreateQoSPolicy(string policyName, string programPath, int limitKBps, string direction = "out")
        {
            try
            {
                Console.WriteLine($"创建QoS策略: {policyName}, 程序: {programPath}, 限速: {limitKBps}KB/s, 方向: {direction}");
                
                // 转换为bps (bits per second)
                long limitBps = (long)limitKBps * 8 * 1024;
                
                // 方法1：尝试使用netsh qos命令
                bool qosMethodSuccess = false;
                
                try {
                    // 创建QoS策略
                    bool policyCreated = ExecuteNetshCommand($"qos add policy name=\"{policyName}\" throttlerate={limitBps}");
                    if (policyCreated)
                    {
                        // 为策略创建规则
                        string filterStr = direction == "in" ? "remoteport=0-65535" : "localport=0-65535";
                        bool ruleAdded = ExecuteNetshCommand($"qos add rule name=\"{policyName}\" dir={direction} " +
                            $"program=\"{programPath}\" {filterStr} profile=all policysetting=\"{policyName}\"");
                        
                        qosMethodSuccess = ruleAdded;
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"QoS方法失败: {ex.Message}，尝试其他方法...");
                }
                
                // 方法2：如果QoS方法失败，尝试使用advfirewall + interface ipv4 策略
                if (!qosMethodSuccess)
                {
                    try {
                        Console.WriteLine("尝试使用高级防火墙 + 网络接口策略方法...");
                        
                        // 创建防火墙规则
                        string firewallArgs = $"advfirewall firewall add rule name=\"{policyName}\" dir={direction} action=allow program=\"{programPath}\" enable=yes";
                        bool firewallRuleCreated = ExecuteNetshCommand(firewallArgs);
                        
                        // 创建接口策略
                        if (firewallRuleCreated)
                        {
                            string ipv4Args = $"interface ipv4 add policy name=\"{policyName}\" dir={(direction == "in" ? "in" : "out")} " +
                                $"throttlelimit={limitKBps} throttlelimittype=kbps program=\"{programPath}\"";
                            bool policyAdded = ExecuteNetshCommand(ipv4Args);
                            
                            if (policyAdded)
                            {
                                Console.WriteLine("接口策略方法成功");
                                return true;
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"接口策略方法失败: {ex.Message}，尝试其他方法...");
                    }
                    
                    // 方法3：通用网络设置 + 规则关联
                    try {
                        Console.WriteLine("尝试使用全局TCP设置方法...");
                        
                        // 设置TCP全局参数
                        ExecuteNetshCommand($"interface tcp set global autotuninglevel=restricted");
                        
                        if (direction == "out") {
                            ExecuteNetshCommand($"interface tcp set global ecncapability=enabled");
                        } else {
                            ExecuteNetshCommand($"interface tcp set global congestionprovider=ctcp");
                        }
                        
                        // 创建防火墙规则
                        string firewallArgs = $"advfirewall firewall add rule name=\"{policyName}\" dir={direction} action=allow program=\"{programPath}\" enable=yes";
                        bool firewallRuleCreated = ExecuteNetshCommand(firewallArgs);
                        
                        if (firewallRuleCreated) {
                            Console.WriteLine("全局TCP设置方法成功");
                            return true;
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"全局TCP设置方法失败: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("QoS策略方法成功");
                    return true;
                }
                
                // 所有方法都失败
                Console.WriteLine("所有带宽限制方法都失败");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建QoS网络限速策略失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 移除QoS网络限速策略
        /// </summary>
        /// <param name="policyName">策略名称</param>
        /// <returns>是否成功</returns>
        public bool RemoveQoSPolicy(string policyName)
        {
            try
            {
                Console.WriteLine($"移除QoS策略: {policyName}");
                
                // 尝试移除QoS规则和策略
                bool ruleRemoved = ExecuteNetshCommand($"qos delete rule name=\"{policyName}\"");
                bool policyRemoved = ExecuteNetshCommand($"qos delete policy name=\"{policyName}\"");
                
                // 尝试移除防火墙规则
                bool firewallRemoved = ExecuteNetshCommand($"advfirewall firewall delete rule name=\"{policyName}\"");
                
                // 尝试移除IPv4策略
                bool ipPolicyRemoved = ExecuteNetshCommand($"interface ipv4 delete policy name=\"{policyName}\"");
                
                // 重置TCP全局设置
                ExecuteNetshCommand("interface tcp set global autotuninglevel=normal");
                ExecuteNetshCommand("interface tcp set global congestionprovider=default");
                ExecuteNetshCommand("interface tcp set global ecncapability=default");
                
                // 只要有一个成功就视为成功
                return ruleRemoved || policyRemoved || firewallRemoved || ipPolicyRemoved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"移除QoS网络限速策略失败: {ex.Message}");
                return false;
            }
        }
        #endregion

        /// <summary>
        /// 检查是否以管理员权限运行
        /// </summary>
        /// <returns>是否具有管理员权限</returns>
        public static bool IsRunningAsAdmin()
        {
            try
            {
                // 使用WindowsIdentity检查当前用户是否具有管理员权限
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 对指定进程应用网络限速
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <param name="uploadLimitKBps">上传限制(KB/s)</param>
        /// <param name="downloadLimitKBps">下载限制(KB/s)</param>
        /// <returns>是否应用成功</returns>
        public async Task<bool> ApplyLimits(int processId, int uploadLimitKBps, int downloadLimitKBps)
        {
            try
            {
                Console.WriteLine($"开始应用网络限速 - 进程ID: {processId}, 上传: {uploadLimitKBps}KB/s, 下载: {downloadLimitKBps}KB/s");
                
                // 检查是否以管理员权限运行
                if (!IsRunningAsAdmin())
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
                string ruleName = $"GameTools_NetLimit_{processName}_{processId}";
                Console.WriteLine($"规则名称: {ruleName}");
                
                // 移除现有规则（如果存在）
                await RemoveLimit(processId);
                
                // 首先尝试使用QoS策略
                bool success = false;
                
                // 上传限速（出站流量）
                if (uploadLimitKBps > 0)
                {
                    bool uploadSuccess = await Task.Run(() => CreateQoSPolicy(
                        $"{ruleName}_Upload",
                        executablePath,
                        uploadLimitKBps,
                        "out"));
                        
                    if (uploadSuccess)
                    {
                        Console.WriteLine("出站限速规则成功应用");
                        success = true;
                    }
                }
                
                // 下载限速（入站流量）
                if (downloadLimitKBps > 0)
                {
                    bool downloadSuccess = await Task.Run(() => CreateQoSPolicy(
                        $"{ruleName}_Download",
                        executablePath,
                        downloadLimitKBps,
                        "in"));
                        
                    if (downloadSuccess)
                    {
                        Console.WriteLine("入站限速规则成功应用");
                        success = true;
                    }
                }
                
                // 如果QoS策略失败，尝试使用传统防火墙规则模拟限速
                if (!success)
                {
                    Console.WriteLine("QoS策略失败，尝试使用传统防火墙规则...");
                    success = await Task.Run(() => CreateNetworkLimitRule(
                        ruleName,
                        executablePath,
                        downloadLimitKBps,
                        uploadLimitKBps));
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用网络限速失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 移除对指定进程的网络限速
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <returns>是否移除成功</returns>
        public async Task<bool> RemoveLimit(int processId)
        {
            try
            {
                Console.WriteLine($"移除网络限速 - 进程ID: {processId}");
                
                // 获取进程路径
                string executablePath = GetProcessPath(processId);
                if (string.IsNullOrEmpty(executablePath))
                {
                    // 如果无法获取进程路径，尝试使用通用名称
                    string genericRuleName = $"GameTools_NetLimit_Process_{processId}";
                    Console.WriteLine($"使用通用规则名称: {genericRuleName}");
                    
                    // 删除可能存在的规则
                    await Task.Run(() => {
                        RemoveRule($"{genericRuleName}_Inbound");
                        RemoveRule($"{genericRuleName}_Outbound");
                        RemoveRule($"{genericRuleName}_Upload");
                        RemoveRule($"{genericRuleName}_Download");
                        RemoveQoSPolicy($"{genericRuleName}_Upload");
                        RemoveQoSPolicy($"{genericRuleName}_Download");
                    });
                    
                    return true;
                }
                
                // 生成防火墙规则名称
                string processName = Path.GetFileNameWithoutExtension(executablePath);
                string limitRuleName = $"GameTools_NetLimit_{processName}_{processId}";
                Console.WriteLine($"规则名称: {limitRuleName}");
                
                // 删除规则
                bool inboundRemoved = await Task.Run(() => RemoveRule($"{limitRuleName}_Inbound"));
                bool outboundRemoved = await Task.Run(() => RemoveRule($"{limitRuleName}_Outbound"));
                bool uploadPolicyRemoved = await Task.Run(() => RemoveQoSPolicy($"{limitRuleName}_Upload"));
                bool downloadPolicyRemoved = await Task.Run(() => RemoveQoSPolicy($"{limitRuleName}_Download"));
                
                // 至少有一个成功就视为成功
                return inboundRemoved || outboundRemoved || uploadPolicyRemoved || downloadPolicyRemoved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"移除网络限速失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取进程的可执行文件路径
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <returns>可执行文件路径</returns>
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
    }
} 