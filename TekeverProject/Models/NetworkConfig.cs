using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TekeverProject.Models
{
    public static class NetworkConfig
    {
        public static bool IsValidIPv4(string ip)
        {
            return !string.IsNullOrWhiteSpace(ip)
                    && Regex.IsMatch(ip, @"^(25[0-5]|2[0-4][0-9]|1?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|1?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|1?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|1?[0-9][0-9]?)$");
        }

        public static bool IsValidGateway(string gateway)
        {
            return IsValidIPv4(gateway) && gateway != "0.0.0.0" && gateway != "255.255.255.255";
        }

        public static bool IsValidSubnetMask(string subnet)
        {
            return !string.IsNullOrWhiteSpace(subnet)
                    && Regex.IsMatch(subnet, @"^(255|254|252|248|240|224|192|128|0)\.(255|252|248|240|224|192|128|0|0)\.(255|252|248|240|224|192|128|0|0|0)\.(252|248|240|224|192|128|0|0|0|0)$");
        }

        public static bool IsValidDNS(string dns)
        {
            return !string.IsNullOrWhiteSpace(dns)
                    && Regex.IsMatch(dns, @"^(?!0)(?!.*\.$)((25[0-5]|2[0-4][0-9]|1?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|1?[0-9][0-9]?)$");
        }

        public static bool AddIPv4ToInterface(string networkInterface, string newIP, string subnetMask)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"netsh interface ip add address '{networkInterface}' {newIP} {subnetMask}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DeleteIPv4ToInterface(string networkInterface, string ip)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"netsh interface ip delete address '{networkInterface}' {ip}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool SetDNS(string networkInterface, string[] dnsServers)
        {
            //Windows Management Instrumentation (WMI)
            //This is used in order to change DNS instantly and seamlessly

            try
            {
                string dnsString = string.Join(",", dnsServers);
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionID != NULL");

                foreach (ManagementObject adapter in searcher.Get())
                {
                    string adapterName = adapter["NetConnectionID"].ToString();
                    if (adapterName == networkInterface)
                    {
                        ManagementObjectSearcher settingsSearcher = new ManagementObjectSearcher(
                            $"SELECT * FROM Win32_NetworkAdapterConfiguration WHERE Index={adapter["Index"]}");

                        foreach (ManagementObject mo in settingsSearcher.Get())
                        {
                            if ((bool)mo["IPEnabled"])
                            {
                                ManagementBaseObject dnsMethod = mo.GetMethodParameters("SetDNSServerSearchOrder");
                                dnsMethod["DNSServerSearchOrder"] = dnsServers;
                                ManagementBaseObject dnsResult = mo.InvokeMethod("SetDNSServerSearchOrder", dnsMethod, null);

                                if (dnsResult != null && (uint)dnsResult["ReturnValue"] == 0)
                                    return true;
                                else
                                    return false;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        public static bool SetGateway(string networkInterface, string gateway, int metric = 1)
        {
            //Windows Management Instrumentation (WMI)
            //This is used in order to change Default Gateway without clearing all existing IPs

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionID IS NOT NULL");

                foreach (ManagementObject adapter in searcher.Get())
                {
                    string adapterName = adapter["NetConnectionID"].ToString();
                    if (adapterName == networkInterface)
                    {
                        ManagementObjectSearcher settingsSearcher = new ManagementObjectSearcher(
                            $"SELECT * FROM Win32_NetworkAdapterConfiguration WHERE Index={adapter["Index"]}");

                        foreach (ManagementObject mo in settingsSearcher.Get())
                        {
                            if ((bool)mo["IPEnabled"])
                            {
                                // Set Gateway
                                ManagementBaseObject gatewayMethod = mo.GetMethodParameters("SetGateways");
                                gatewayMethod["DefaultIPGateway"] = new string[] { gateway };
                                gatewayMethod["GatewayCostMetric"] = new int[] { metric };

                                ManagementBaseObject gatewayResult = mo.InvokeMethod("SetGateways", gatewayMethod, null);

                                if (gatewayResult != null && (uint)gatewayResult["ReturnValue"] == 0)
                                    return true;
                                else
                                    return false;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }
}