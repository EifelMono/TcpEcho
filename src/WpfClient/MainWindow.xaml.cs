using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EifelMono.Fluent;
using EifelMono.Fluent.Bindings;

namespace WpfClient
{
    public partial class MainWindow : Window, INotifyPropertyChanged, IOnPropertyChanged
    {
        #region Bindings
        Action<Action> action;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Title = $"WpfClient {fluent.App.TargetFrameworkName}";
            action= (a) => Application.Current.Dispatcher.Invoke(a);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string propertyName = "") =>
            (PropertyChanged)?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region Connect

        Socket ClientSocket;

        private string _ConnectStatus = null;
        public string ConnectStatus
        {
            get => this.PropertyGet(ref _ConnectStatus);
            set => this.PropertySet(ref _ConnectStatus, value);
        }

        private ICommand _CommandConnect;
        public ICommand CommandConnect
            => this.Command(ref _CommandConnect, async () =>
            {
                ConnectStatus = "Connecting to port 8087";
                try
                {
                    ClientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await ClientSocket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 8087));
                    ConnectStatus = "Connected";
                }
                catch (Exception ex)
                {
                    ClientSocket.Dispose();
                    ConnectStatus = "not Connected";
                    MessageBox.Show($"not Connected {ex.Message}");
                }
            });

        #endregion

        #region Send
        private async Task SendMessageAsync(int count, int sendBlockSize = -1, bool withNewLine = true)
        {
            try
            {
                var sendText = new string('a', count) + (withNewLine ? Environment.NewLine : "");
                if (sendBlockSize == -1)
                    sendBlockSize = sendText.Length;
                while (sendText.Length > 0)
                {
                    var lenSend = sendText.Length > sendBlockSize ? sendBlockSize : sendText.Length;
                    var buffer = Encoding.ASCII.GetBytes(sendText.Substring(0, lenSend));
                    await ClientSocket.SendAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    sendText = sendText.Substring(lenSend, sendText.Length - lenSend);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"not sent {ex.Message}");
            }
        }
        private ICommand _CommandSend;
        public ICommand CommandSend
            => this.Command(ref _CommandSend, async (parameter) =>
                {
                    var args = (parameter as string ?? "").Split('|');
                    if (args.Length != 3) return;
                    await SendMessageAsync(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]), args[2].ToLower() == "newline");
                });
        #endregion
    }
}
