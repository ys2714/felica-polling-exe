// See https://aka.ms/new-console-template for more information

using System;
using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Info;
using LibUsbDotNet.Descriptors;
using System.Threading;

// File Transfer Mode: PID=0x2104
// PTP Mode: PID=0x2109
class Program
{
    // These are the standard VID/PID for Google when in Accessory Mode 0x18D1.
    private const int ACCESSORY_VID = 0x05E0;
    // Use 0x2D01 for accessory with ADB enabled, or 0x2D00 for accessory-only mode.
    private const int ACCESSORY_PID = 0x2104;

    private const int ACCESSORY_REVISION = 0x0504;

    // The UsbDevice object that represents our connected Android device
    public static IUsbDevice MyUsbDevice;

    static void Main(string[] args)
    {
        Console.WriteLine($"Searching for Android USB accessory (VID:{ACCESSORY_VID.ToString("X4")}, PID:{ACCESSORY_PID.ToString("X4")})...");
        
        // Finder object to locate our device
        // var finder = new UsbDeviceFinder(ACCESSORY_VID, ACCESSORY_PID, ACCESSORY_REVISION);

        UsbEndpointReader reader = null;
        UsbEndpointWriter writer = null;

        var context = new UsbContext();

        try
        {
            // Loop until the device is found
            while (true)
            {
                // Open the device found by the finder
                // MyUsbDevice = UsbDevice.OpenUsbDevice(finder);

                MyUsbDevice = context.List().FirstOrDefault(d => d.ProductId == ACCESSORY_PID && d.VendorId == ACCESSORY_VID);

                // If the device is open and ready
                // && MyUsbDevice.IsOpen
                if (MyUsbDevice != null)
                {
                    Console.WriteLine("\nDevice found! Opening connection...");
                    MyUsbDevice.Open();
                    break; // Exit the loop
                }

                Console.Write(".");
                Thread.Sleep(1000); // Wait and retry
            }

            // If this is a "whole" usb device (libusb-win32, linux, mac).
            // If it is a WinUSB device, get the usb device interface instead.
            IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
            if (!ReferenceEquals(wholeUsbDevice, null))
            {
                // This is a "whole" USB device. Before it can be used, 
                // the desired configuration and interface must be selected.
                wholeUsbDevice.SetConfiguration(1);
                wholeUsbDevice.ClaimInterface(0);
            }

            // Open the reader and writer endpoints.
            // These are the communication channels.
            reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
            writer = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);

            if (reader == null || writer == null)
            {
                throw new Exception("Could not open read/write endpoints.");
            }
            
            Console.WriteLine("Connection successful!");

            // --- Sending a message to the Android app ---
            Console.WriteLine("Enter a message to send to the Android device:");
            string messageToSend = Console.ReadLine();
            byte[] dataToSend = Encoding.UTF8.GetBytes(messageToSend);
            
            Error ec = writer.Write(dataToSend, 2000, out int bytesWritten);
            if (ec != Error.Success) throw new Exception("Write Error: " + ec);
            
            Console.WriteLine($"Sent {bytesWritten} bytes: {messageToSend}");

            // --- Receiving a response from the Android app ---
            Console.WriteLine("Waiting for response...");
            byte[] readBuffer = new byte[512]; // Buffer to hold the received data
            
            ec = reader.Read(readBuffer, 2000, out int bytesRead);
            if (ec != Error.Success) throw new Exception("Read Error: " + ec);

            if (bytesRead > 0)
            {
                string responseMessage = Encoding.UTF8.GetString(readBuffer, 0, bytesRead);
                Console.WriteLine($"Received {bytesRead} bytes: {responseMessage}");
            }
            else
            {
                Console.WriteLine("No response received within the timeout.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("\nError: " + (ex.InnerException ?? ex).Message);
        }
        finally
        {
            if (MyUsbDevice != null && MyUsbDevice.IsOpen)
            {
                // Close the device
                MyUsbDevice.Close();
            }
            // Free usb resources
            // UsbDevice.Exit(); 
        }

        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }
}
