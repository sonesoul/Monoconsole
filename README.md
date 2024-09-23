# Monoconsole
Monoconsole is a lightweight library for opening and managing a console window in non-console applications.
It allows interaction with the console, handles user input, displays messages with customizable colors, and executes commands asynchronously.


The console integrates with `System.Console` in C#, meaning that once it's open, you can use `Console.WriteLine` or any other Console methods/properties seamlessly.
However, it's recommended to use `Monoconsole.Write` and `Monoconsole.WriteLine` primarily

### Installation
Reference the compiled .dll file in your project:
In Visual Studio, right-click on your project in the Solution Explorer, choose "Add Reference...", and locate the .dll file.

Add the using directive in the files where you want to use the library:
```csharp
using MonoconsoleLib;
```

### Open/Close
You can open the console by calling:
```csharp
Monoconsole.Open();
```
To close the console:
```csharp
Monoconsole.Close();
```
You can toggle the console open/close state:
```csharp
Monoconsole.Toggle();
```
And you can recreate console window:
```csharp
Monoconsole.New();
```

### Logic
You can assign an input handler for processing input in a basic console logic and the `Execute`/`ExecuteAsync` methods:
```csharp
Monoconsole.Handler = (input) => 
{
    Monoconsole.WriteLine($"Received: {input}");
};
```

To execute commands either synchronously or asynchronously:

```csharp
Monoconsole.Execute("your command"); //blocks the thread on which it was called
await Monoconsole.ExecuteAsync("your async command");
```

By default, Monoconsole uses read-line loop that redirects input to the `Monoconsole.Handler`, but you can customize that logic to fit your needs::

```csharp
Monoconsole.MainTask = (args, token) =>
{
    string input;
    do
    {
        input = Monoconsole.ReadLine();
    }
    while (input != "exit");
    //when the task is completed, the console closes
};
Monoconsole.Open();
Monoconsole.MainTask = null; //set to default
```
The `Open` and `New` methods can accept args that will be redirected to your console logic:

```csharp
Monoconsole.MainTask = (args, token) =>
{
    if (args.Length > 0)
    {
        Monoconsole.WriteLine(args[0]);
    }
    Monoconsole.ReadLine();
};
Monoconsole.Open(new string[] { "first", "second" });
//writes "first"
```

### Colors
You can modify text colors by setting:
```csharp
Monoconsole.ForeColor = ConsoleColor.Green; //sets the color of characters
Monoconsole.BackColor = ConsoleColor.White; //sets the background color of characters 
Monoconsole.InfoColor = ConsoleColor.Blue; //sets the color that used in the Monoconsole.WriteInfo
Monoconsole.ErrorColor = ConsoleColor.Magenta; //sets the color that used in the Monoconsole.WriteError
```
And you can pass the color as an argument:
```csharp
Monoconsole.WriteLine("Red color", ConsoleColor.Red);
//writes "Red color" using a red color

Monoconsole.Write("Blue color", ConsoleColor.Blue);
//writes "Blue color" using a blue color
```

### Events
MMonoconsole provides several events, such as:

`Opened` – Triggered when the console opens.

`Closed` – Triggered when the console closes.

`InputReceived` – Triggered when input is received using a `Monoconsole.ReadLine()`.

```csharp
Monoconsole.InputReceived += (input) => 
{
    Monoconsole.WriteLine($"Command executed: {input}");
};
```
## Known Issue
When the console window is closed manually or the process is terminated (e.g., through Task Manager), 
the **main application thread is also terminated**. This happens because the console process is linked to the main application thread, and closing it forces the entire application to stop.
To avoid this, **do not close the console manually** — use `Monoconsole.Close()` instead. By default, the console window has no buttons 
(such as close or fullscreen) and does not appear in the taskbar.  You can customize button visibility or show the console window in the taskbar, but do so at your own risk:
```csharp
Monoconsole.HideFromTaskbar = false; //enables showing in taskbar
Monoconsole.HideButtons = false; //enables buttons for the window
```
