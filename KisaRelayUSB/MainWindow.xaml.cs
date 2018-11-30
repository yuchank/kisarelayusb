using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Quobject.SocketIoClientDotNet.Client;
using System.Runtime.InteropServices;
using System.Windows.Interop;

using System.Windows.Threading;

using System.IO;

namespace KisaRelayUSB
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private Socket socket;
    private YRelay relay1;
    private YRelay relay2;

    private DispatcherTimer timer;

    public MainWindow()
    {
      InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      this.socket = IO.Socket("http://192.168.1.2:2000");

      timer = new DispatcherTimer();
      timer.Interval = TimeSpan.FromMilliseconds(800);  // 2 sec
      timer.Tick += new EventHandler(timer_Tick);

      if (Window.GetWindow(this) != null)
      {
        WindowInteropHelper helper = new WindowInteropHelper(Window.GetWindow(this));
        HwndSource.FromHwnd(helper.Handle).AddHook(new HwndSourceHook(this.WndProc));
      }

      this.socket.On(Socket.EVENT_CONNECT_ERROR, () => {
        Console.WriteLine("EVT_CON_ERR");
      });

      this.socket.On(Socket.EVENT_CONNECT_TIMEOUT, () => {
        Console.WriteLine("EVT_CON_TOUT");
      });

      this.socket.On(Socket.EVENT_CONNECT, () => {
        Console.WriteLine("EVT_CON");
      });

      this.socket.On(Socket.EVENT_DISCONNECT, () => {
        Console.WriteLine("EVT_DISCON");
      });

      this.socket.On("attack-app-cs", () =>
      {
        timer.Start();
      });

      this.socket.On("attack-obd-cs", () =>
      {
        timer.Start();
      });

      this.socket.On("attack-auto-cs", () =>
      {
        timer.Start();
      });

      this.socket.On("attack-usb-cs", () =>
      {
        timer.Start();
      });

      this.socket.On("attack-rans-cs", () =>
      {
        timer.Start();
      });

      this.socket.On("check-usb", (v) =>
      {
        String command = v.ToString();
        Boolean found = false;
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
          if (drive.DriveType == DriveType.Removable)
          {
            Console.WriteLine(string.Format("({0}) {1}", drive.Name.Replace("\\", ""), drive.VolumeLabel));
            found = true;
          }
        }

        if (found == true)
        {
          if (command.Equals("check-usb-usb"))
          {
            this.socket.Emit("usb-status", "usb-on");
          }
          else if (command.Equals("check-rans-usb"))
          {
            this.socket.Emit("usb-status", "rans-on");
          }
        }
        else
        {
          this.socket.Emit("usb-status", "off");
        }
      });

      this.socket.On("reset", () =>
      {
        timer.Stop();
        ResetRelay();
      });

      string errmsg = "";

      if (YAPI.RegisterHub("usb", ref errmsg) != YAPI.SUCCESS)
      {
        MessageBox.Show("RegisterHub error: " + errmsg);
        Environment.Exit(0);
      }

      relay1 = YRelay.FindRelay("RELAYLO1-CD6A7.relay1");
      relay2 = YRelay.FindRelay("RELAYLO1-CD6A7.relay2");
    }

    private void ResetRelay()
    {
      if (relay1.isOnline())
      {
        relay1.set_state(YRelay.STATE_A);  // idle
      }

      if (relay2.isOnline())
      {
        relay2.set_state(YRelay.STATE_A);  // idle
      }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
      this.socket.Disconnect();
      this.socket.Close();
      this.timer.Stop();
      ResetRelay();
    }

    private void timer_Tick(object sender, EventArgs e)
    {
      if (relay1.isOnline())
      {
        if (relay1.get_state() == YRelay.STATE_B)       // active
        {
          relay1.set_state(YRelay.STATE_A);
        }
        else if (relay1.get_state() == YRelay.STATE_A)  // idle
        {
          relay1.set_state(YRelay.STATE_B);
        }
      }

      if (relay2.isOnline())
      {
        if (relay2.get_state() == YRelay.STATE_B)       // active
        {
          relay2.set_state(YRelay.STATE_A);
        }
        else if (relay2.get_state() == YRelay.STATE_A)  // idle
        {
          relay2.set_state(YRelay.STATE_B);
        }
      }
    }

    IntPtr WndProc(IntPtr hWnd, int nMsg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
      UInt32 WM_DEVICECHANGE = 0x0219;
      UInt32 DBT_DEVTUP_VOLUME = 0x02;
      UInt32 DBT_DEVICEARRIVAL = 0x8000;
      UInt32 DBT_DEVICEREMOVECOMPLETE = 0x8004;

      // USB IN
      if ((nMsg == WM_DEVICECHANGE) && (wParam.ToInt32() == DBT_DEVICEARRIVAL))
      {
        int devType = Marshal.ReadInt32(lParam, 4);
        if (devType == DBT_DEVTUP_VOLUME)
        {
        }
      }

      // USB OUT
      if ((nMsg == WM_DEVICECHANGE) && (wParam.ToInt32() == DBT_DEVICEREMOVECOMPLETE))
      {
        int devType = Marshal.ReadInt32(lParam, 4);
        if (devType == DBT_DEVTUP_VOLUME)
        {
        }
      }
      return IntPtr.Zero;
    }

    private void OnAppClick(object sender, RoutedEventArgs e)
    {
      this.socket.Emit("attack-app", "attack-app");
    }

    private void OnObdClick(object sender, RoutedEventArgs e)
    {
      this.socket.Emit("attack-obd", "attack-obd");
    }

    private void OnAutoClick(object sender, RoutedEventArgs e)
    {
      this.socket.Emit("attack-auto", "attack-auto");
    }

    private void OnUsbClick(object sender, RoutedEventArgs e)
    {
      this.socket.Emit("attack-usb", "attack-usb");
    }

    private void OnRansClick(object sender, RoutedEventArgs e)
    {
      this.socket.Emit("attack-rans", "attack-rans");
    }

    private void OnAutosetClick(object sender, RoutedEventArgs e)
    {
      this.socket.Emit("auto-on", "auto-on");
    }

    private void OnRstClick(object sender, RoutedEventArgs e)
    {
      this.socket.Emit("reset", "reset");
    }
  }
}
