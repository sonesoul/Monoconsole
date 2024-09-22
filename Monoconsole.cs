using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MonoconsoleLib.NativeInterop;

namespace MonoconsoleLib
{
    /// <summary>
    /// Provides methods and properties to manage a simple console window.
    /// </summary>
    public static class Monoconsole
    {
        /// <summary>
        /// Gets or sets the foreground color of the text in the console.
        /// </summary>
        public static ConsoleColor ForeColor { get => Console.ForegroundColor; set => Console.ForegroundColor = value; }
        /// <summary>
        /// Gets or sets the background color of the text in the console.
        /// </summary>
        public static ConsoleColor BackColor { get => Console.BackgroundColor; set => Console.BackgroundColor = value; }
        /// <summary>
        /// Gets or sets the color used for informational messages.
        /// </summary>
        /// <value>The color for informational messages. Default is Cyan.</value>
        public static ConsoleColor InfoColor { get; set; } = ConsoleColor.Cyan;
        /// <summary>
        /// Gets or sets the color used for error messages.
        /// </summary>
        /// <value>The color for error messages. Default is Red.</value>
        public static ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;

        /// <summary>
        /// Gets a value indicating whether the console is currently opened.
        /// </summary>
        /// <returns>true if the console is opened; otherwise, false.</returns>
        public static bool IsOpened { get; private set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether to enable or disable writing commands that have been executed.
        /// </summary>
        public static bool WriteExecuted { get; set; } = true;
        /// <summary>
        /// Gets or sets a value indicating whether to hide the console's system buttons.
        /// </summary>
        public static bool HideButtons { get; set; } = true;
        /// <summary>
        /// Gets or sets a value indicating whether to hide the console from the taskbar.
        /// </summary>
        public static bool HideFromTaskbar { get; set; } = true;

        /// <summary>
        /// Gets or sets the title of the console window.
        /// </summary>
        public static string Title { get; set; } = "monoconsole";
        /// <summary>
        /// Gets or sets the basic command that closes the console.
        /// </summary>
        public static string ExitCommand { get; set; } = "exit";

        /// <summary>
        /// Gets or sets handler that receives input from console window. Also receives input from the <see cref="Execute(string)"/> or <see cref="ExecuteAsync(string)"/>
        /// </summary>
        public static Action<string> Handler { get; set; }
        /// <summary>
        /// Gets or sets the handler that receives exceptions thrown in <see cref="Execute(string)"/> or <see cref="ExecuteAsync"/> methods.
        /// </summary>
        public static Action<Exception> ExceptionHandler { get; set; }
        /// <summary>
        /// Gets or sets the task that runs when the console is open. If null, this is a read-line loop that redirects input to the <see cref="Handler"/>.
        /// </summary>
        public static Action<object[], CancellationToken> MainTask { get; set; } = null;

        /// <summary>
        /// Gets a value indicating the current thread of the console.
        /// </summary>
        ///<returns>Current thread of the console if it is opened; otherwise, null.</returns>
        public static Thread WorkingThread { get; private set; } = null;
        /// <summary>
        /// Gets a value indicating the current handle of the console window.
        /// </summary>
        ///<returns>Console window handle of the console if it is opened; otherwise, null.</returns>
        public static IntPtr? WindowHandle { get; private set; } = null;

        /// <summary>
        /// Occurs when the console is opened.
        /// </summary>
        public static event Action Opened;
        /// <summary>
        /// Occurs when the console is closed.
        /// </summary>
        public static event Action Closed;
        /// <summary>
        /// Occurs when input is received from the console.
        /// </summary>
        public static event Action<string> InputReceived;

        private readonly static object _writeLock = new object();

        private static CancellationTokenSource _cts;

        private readonly static TextReader _originalIn = Console.In;
        private readonly static TextWriter _originalOut = Console.Out;
        private readonly static TextWriter _originalErr = Console.Error;

        /// <summary>
        /// Opens the console if it is not already open.
        /// </summary>
        /// <returns>true if the console was successfully opened; otherwise, false.</returns>
        public static bool Open(object[] args = null)
        {
            if (!IsOpened)
            {
                bool result = AllocConsole();

                if (result)
                {
                    SetHandles();
                    ForeColor = ConsoleColor.Yellow;
                    Console.Title = Title;

                    SetWindow();

                    Console.OutputEncoding = Encoding.UTF8;
                    _cts = new CancellationTokenSource();

                    WorkingThread = new Thread(() =>
                    {
                        if (MainTask != null)
                            MainTask.Invoke(args, _cts.Token);
                        else
                            ConsoleRead(_cts.Token);

                        if (WorkingThread == Thread.CurrentThread)
                            Close();
                    });

                    WorkingThread.IsBackground = true;
                    WorkingThread.Start();

                    IsOpened = true;
                    Opened?.Invoke();
                }

                return result;
            }
            return false;
        }
        /// <summary>
        /// Closes the console if open.
        /// </summary>
        /// <returns>true if the console was successfully closed; otherwise, false.</returns>
        public static bool Close()
        {
            if (IsOpened)
            {
                bool result = FreeConsole();

                if (result)
                {
                    IsOpened = false;

                    _cts?.Cancel();

                    ConsoleReset();

                    WorkingThread = null;
                    WindowHandle = null;

                    Closed?.Invoke();
                }

                return result;
            }
            return false;
        }
        /// <summary>
        /// Toggles the console state (opens or closes it).
        /// </summary>
        public static void Toggle()
        {
            if (IsOpened)
                Close();
            else
                Open();
        }
        /// <summary>
        /// Closes the console if it is open and opens a new one.
        /// </summary>
        public static void New()
        {
            if (IsOpened)
                Close();

            Open();
        }

        /// <summary>
        /// Pushes a command to the <see cref="Handler"/>
        /// </summary>
        public static void Execute(string command)
        {
            try
            {
                Task.Run(() =>
                {
                    if (WriteExecuted)
                        WriteLine("> " + command, ConsoleColor.DarkYellow).Wait();

                    Handler?.Invoke(command);
                }).Wait();
            }
            catch (Exception ex)
            {
                WriteError($"{ex.Message} [sync]").Wait();
                ExceptionHandler?.Invoke(ex);
            }

        }
        /// <summary>
        /// Asynchronously pushes a command to the <see cref="Handler"/>
        /// </summary>
        /// <returns>Task representing the asynchronous operation.</returns>
        public static async Task ExecuteAsync(string command)
        {
            try
            {
                if (WriteExecuted)
                    await WriteLine("> " + command, ConsoleColor.Blue);

                await Task.Run(() => Handler?.Invoke(command));
            }
            catch (Exception ex)
            {
                await WriteError($"{ex.Message} [async]");
                ExceptionHandler?.Invoke(ex);
            }
        }
        /// <summary>
        /// Executes a command on the specified synchronization context, handling exceptions if they occur.
        /// </summary>
        public static void ExecuteOnThread(SynchronizationContext context, string command, Action<Exception> errorHandler = null)
        {
            context.Post(_ =>
            {
                try
                {
                    Execute(command);
                }
                catch (Exception ex)
                {
                    errorHandler?.Invoke(ex);
                }
            }, null);
        }

        private static void ConsoleRead(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string input = ReadLine();

                    if (input.Equals(ExitCommand, StringComparison.OrdinalIgnoreCase))
                        Close();

                    if (Handler == null)
                        throw new NullReferenceException("Handler is null.");
                    else
                        Handler.Invoke(input);
                }
                catch (Exception ex)
                {
                    WriteError($"{ex.Message} [read]").Wait(CancellationToken.None);
                }
            }
        }

        private static void SetWindow()
        {
            if (!WindowHandle.HasValue)
                return;

            IntPtr hWnd = WindowHandle.Value;

            if (HideButtons)
            {
                int style = GetWindowLong(hWnd, GWL_STYLE);
                style &= ~WS_SYSMENU;
                _ = SetWindowLong(hWnd, GWL_STYLE, style);
            }

            if (HideFromTaskbar)
            {
                int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                _ = SetWindowLong(hWnd, GWL_EXSTYLE, (exStyle & ~WS_EX_APPWINDOW) | WS_EX_TOOLWINDOW);
            }
        }
        private static void ConsoleReset()
        {
            Console.SetOut(_originalOut);
            Console.SetError(_originalErr);
            Console.SetIn(_originalIn);
        }
        private static void SetHandles()
        {
            Console.SetIn(
                new StreamReader(
                    new FileStream(
                        new SafeFileHandle(GetStdHandle(STD_INPUT_HANDLE), false), FileAccess.Read)));
            Console.SetOut(
                new StreamWriter(
                    new FileStream(
                        new SafeFileHandle(GetStdHandle(STD_OUTPUT_HANDLE), false), FileAccess.Write))
                { AutoFlush = true });
            Console.SetError(
                new StreamWriter(
                    new FileStream(
                        new SafeFileHandle(GetStdHandle(STD_ERROR_HANDLE), false), FileAccess.Write))
                { AutoFlush = true });

            WindowHandle = GetConsoleWindow();
        }


        /// <summary>
        /// Works like <see cref="Console.ReadLine"/> but also invokes <see cref="InputReceived"/>
        /// </summary>
        public static string ReadLine()
        {
            string input = Console.ReadLine();
            InputReceived?.Invoke(input);
            return input;
        }

        private static async Task WriteAsync(Action writeAction, ConsoleColor color = ConsoleColor.White)
        {
            try
            {
                await Task.Run(() =>
                {
                    lock (_writeLock)
                    {
                        if (IsOpened)
                        {
                            var temp = Console.ForegroundColor;
                            ForeColor = color;

                            writeAction();

                            ForeColor = temp;
                        }
                    }
                });
            }
            catch (IOException)
            {
                //Invalid handle exceptions
            }
        }

        /// <summary>
        /// Works like <see cref="WriteLine(string, ConsoleColor)"/> but uses <see cref="InfoColor"/> as second parameter.
        /// </summary>
        /// <returns>Task representing the asynchronous operation.</returns>
        public static Task WriteInfo(string message) => WriteLine(message, InfoColor);
        /// <summary>
        /// Works like <see cref="WriteLine(string, ConsoleColor)"/> but uses <see cref="ErrorColor"/> as second parameter.
        /// </summary>
        /// <returns>Task representing the asynchronous operation.</returns>
        public static Task WriteError(string message) => WriteLine(message, ErrorColor);

        #region WriteLine
        public static Task WriteLine() => WriteAsync(() => Console.WriteLine());
        public static Task WriteLine(bool value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);
        public static Task WriteLine(char value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);
        public static Task WriteLine(char[] buffer, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(buffer), color);
        public static Task WriteLine(decimal value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);
        public static Task WriteLine(double value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);
        public static Task WriteLine(float value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);
        public static Task WriteLine(int value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);
        public static Task WriteLine(long value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);
        public static Task WriteLine(object value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);
        public static Task WriteLine(string value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);
        public static Task WriteLine(uint value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);
        public static Task WriteLine(ulong value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.WriteLine(value), color);

        #endregion

        #region Write
        public static Task Write(bool value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        public static Task Write(char value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        public static Task Write(char[] buffer, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(buffer), color);
        public static Task Write(decimal value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        public static Task Write(double value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        public static Task Write(float value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        public static Task Write(int value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        public static Task Write(long value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        public static Task Write(object value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        public static Task Write(string value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        public static Task Write(uint value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        public static Task Write(ulong value, ConsoleColor color = ConsoleColor.Gray) => WriteAsync(() => Console.Write(value), color);
        #endregion
    }
}