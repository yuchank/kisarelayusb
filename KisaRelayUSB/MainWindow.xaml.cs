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

namespace KisaRelayUSB
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private Socket socket;
    private YRelay relay;

    public MainWindow()
    {
      InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      this.socket = IO.Socket("http://localhost:2000");

      if (Window.GetWindow(this) != null)
      {
        WindowInteropHelper helper = new WindowInteropHelper(Window.GetWindow(this));
        HwndSource.FromHwnd(helper.Handle).AddHook(new HwndSourceHook(this.WndProc));
      }

      this.socket.On(Socket.EVENT_CONNECT, () => {
      });

      this.socket.On(Socket.EVENT_DISCONNECT, () => {
      });

      string errmsg = "";

      if (YAPI.RegisterHub("usb", ref errmsg) != YAPI.SUCCESS)
      {
        MessageBox.Show("RegisterHub error: " + errmsg);
        Environment.Exit(0);
      }

      relay = YRelay.FindRelay("RELAYLO1-CD6A7.relay1");

      this.socket.On("relay-on", () =>
      {
        if (relay.isOnline())
        {
          relay.set_state(YRelay.STATE_B);  // active
        }
      });

      this.socket.On("realy-off", () =>
      {
        if (relay.isOnline())
        {
          relay.set_state(YRelay.STATE_A);  // idle
        }
      });
    }

    private void OnBtnClick(object sender, RoutedEventArgs e)
    {
      if (relay.isOnline())
      {
        if (relay.get_state() == YRelay.STATE_B)       // active
          relay.set_state(YRelay.STATE_A);
        else if (relay.get_state() == YRelay.STATE_A)  // idle
          relay.set_state(YRelay.STATE_B);
      }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
      this.socket.Disconnect();
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
