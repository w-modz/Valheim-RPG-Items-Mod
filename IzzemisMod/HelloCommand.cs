using Jotunn.Entities;

public class HelloCommand : ConsoleCommand
{
    public override string Name => "hello";

    public override string Help => "Hello World!";

    public override void Run(string[] args)
    {
        string message = args.Length == 0 ? "Hello World!" : string.Join(" ", args);
        Console.instance.Print(message);
    }
}