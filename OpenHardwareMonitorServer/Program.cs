using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenHardwareMonitor.Hardware;

namespace OpenHardwareMonitorServer
{
    internal class Program
    {

        static Computer computer = new Computer();
        static void Main(string[] args)
        {

            
            Process currentProcess = Process.GetCurrentProcess();
            currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

            computer = new Computer();
            computer.Open();
            computer.GPUEnabled = true;
            computer.CPUEnabled = true;


            if (!true)
            {
                DoCpuTest();
            }




            string url = "http://localhost/mini-monitor-data/";

            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(url);
                listener.Start();

                try
                {
                    while (true)
                    {
                        HttpListenerContext context = listener.GetContext();

                        HandleRequest(context);

                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
                finally
                {
                    listener.Stop();
                }
            }

            while (!true)
            {


                Console.WriteLine();
                if (true)
                {

                    foreach (var hardware in computer.Hardware.Where(x => x.HardwareType == HardwareType.GpuNvidia))
                    {

                        var temp = hardware.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Temperature).Value;
                        var load = hardware.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Load).Value;
                        var coreClock = hardware.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Clock).Value;
                        var memClock = hardware.Sensors.FirstOrDefault(s => s.Name == "GPU Memory" && s.SensorType == SensorType.Clock).Value;
                        var fanPercent = hardware.Sensors.FirstOrDefault(s => s.Name == "GPU Fan" && s.SensorType == SensorType.Control).Value;
                        var fanRpm = hardware.Sensors.FirstOrDefault(s => s.Name == "GPU" && s.SensorType == SensorType.Fan).Value;

                        Console.WriteLine($"temp{temp}");

                        // has
                        /*
                    *GPU Core Temperature: 33
                    *GPU Core Load: 10
                    *GPU Core Clock: 840.0001
                    *GPU Memory Clock: 810.0001
                    *GPU Fan Control: 20
                    *GPU Fan : 700

                    GPU Shader Clock: 0        
                    GPU Frame Buffer Load : 9
                    GPU Video Engine Load : 2
                    GPU Bus Interface Load : 0

                    GPU Memory Total SmallData : 11264
                    GPU Memory Used SmallData : 3524.867
                    GPU Memory Free SmallData : 7739.133
                    GPU Memory Load: 31.29321
                        */
                        foreach (var sensor in hardware.Sensors)
                        {


                            //Console.WriteLine($"\t{sensor.Name} {sensor.SensorType} : {sensor.Value}");
                        }
                        hardware.Update();
                    }
                }

                //Console.WriteLine(computer.GetReport());
                Thread.Sleep(1000);
            }




        }

        private static void DoCpuTest()
        {

            var gpu = computer.Hardware.First(x => x.HardwareType == HardwareType.GpuNvidia);
            var cpu = computer.Hardware.First(x => x.HardwareType == HardwareType.CPU);

            using (PerformanceCounter cpuInfoCounter = new PerformanceCounter("Processor Information", "% Processor Time", "_Total"))
            {

                using (PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    while (true)
                    {
                        gpu.Update();
                        cpu.Update();

                        Console.WriteLine();
                        Console.WriteLine(cpuCounter.CounterName + " " + cpuCounter.NextValue() + " / " + cpuInfoCounter.NextValue());

                        float? total = 0;
                        float coreCount = 0;
                        foreach (var s in cpu.Sensors.Where(x => x.SensorType == SensorType.Load))
                        {

                            if (s.Name != "CPU Total")
                            {
                                total += s.Value;
                                coreCount++;
                            }
                            Console.WriteLine(s.Name + " " + s.SensorType + " - " + s.Value);

                        }
                        Console.WriteLine("*Cores Total " + total / coreCount);
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        static void HandleRequest(HttpListenerContext context)
        {
            // You can customize the handling of the request here
            //Console.WriteLine($"Received request: {context.Request.HttpMethod} {context.Request.RawUrl}");

            context.Response.Headers["Access-Control-Allow"] = "*";
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type,Accept,isajax";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS";

            var gpu = computer.Hardware.First(x => x.HardwareType == HardwareType.GpuNvidia);
            var cpu = computer.Hardware.First(x => x.HardwareType == HardwareType.CPU);

            gpu.Update();
            cpu.Update();

            var cpuTotal = cpu.Sensors.FirstOrDefault(y => y.Name == "CPU Total");

            //float? coreTotal = 0;
            //foreach (var s in cpu.Sensors.Where(x => x.SensorType == SensorType.Load)) {
            //  if (s.Name != "CPU Total") {
            //    coreTotal += s.Value;
            //  }
            //}



            var response = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                temp = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Temperature).Value
              ,
                load = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Load).Value
              ,
                coreClock = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Clock).Value
              ,
                memClock = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Memory" && s.SensorType == SensorType.Clock).Value
              ,
                fanPercent = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Fan" && s.SensorType == SensorType.Control).Value
              ,
                fanRpm = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU" && s.SensorType == SensorType.Fan).Value
              ,
                cpuTotal = cpuTotal.Value
            });

            //Console.WriteLine($"temp{temp}");

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(response);

            // Set the content type and length
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;

            // Write the response
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        }
    }
}
