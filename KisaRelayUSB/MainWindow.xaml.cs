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

    //private DispatcherTimer timer;

    public MainWindow()
    {
      InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      this.socket = IO.Socket("http://localhost:2000");

      //timer = new DispatcherTimer();
      //timer.Interval = TimeSpan.FromMilliseconds(1);  // 1 sec
      //timer.Tick += new EventHandler(timer_Tick);
      //timer.Start();

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

      string errmsg = "";

      if (YAPI.RegisterHub("usb", ref errmsg) != YAPI.SUCCESS)
      {
        MessageBox.Show("RegisterHub error: " + errmsg);
        Environment.Exit(0);
      }

      relay1 = YRelay.FindRelay("RELAYLO1-CD6A7.relay1");
      relay2 = YRelay.FindRelay("RELAYLO1-CD6A7.relay2");

      this.socket.On("relay-on", () =>
      {
        if (relay1.isOnline() && relay2.isOnline())
        {
          relay1.set_state(YRelay.STATE_B);  // active
          relay2.set_state(YRelay.STATE_B);  // active
        }
      });

      this.socket.On("realy-off", () =>
      {
        if (relay1.isOnline() && relay2.isOnline())
        {
          relay1.set_state(YRelay.STATE_A);  // idle
          relay2.set_state(YRelay.STATE_A);  // idle
        }
      });
    }

    private void OnBtnClick(object sender, RoutedEventArgs e)
    {
      if (relay1.isOnline() && relay2.isOnline())
      {
        if (relay1.get_state() == YRelay.STATE_B && relay2.get_state() == YRelay.STATE_B)       // active
        {
          relay1.set_state(YRelay.STATE_A);
          relay2.set_state(YRelay.STATE_A);
        }
        else if (relay1.get_state() == YRelay.STATE_A && relay2.get_state() == YRelay.STATE_A)  // idle
        {
          relay1.set_state(YRelay.STATE_B);
          relay2.set_state(YRelay.STATE_B);
        }
      }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
      this.socket.Disconnect();
      this.socket.Close();
      //this.timer.Stop();
    }

    //private void timer_Tick(object sender, EventArgs e)
    //{
    //  this.socket.Emit("ping", "ping");
    //}

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
          this.socket.Emit("usb-in", "usb-in");
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
  }
}
