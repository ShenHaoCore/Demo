Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== Demo.Delegates.Console：委托与事件演示 ===\n");

DemoCustomDelegate();
Console.WriteLine();
DemoMulticast();
Console.WriteLine();
DemoActionFunc();
Console.WriteLine();
DemoEvent();

Console.WriteLine("\n全部演示完成。");

static void DemoCustomDelegate()
{
    Console.WriteLine("【1】自定义委托");
    IntOperation add = (a, b) => a + b;
    IntOperation mul = (a, b) => a * b;
    Console.WriteLine($"  add(3,4) = {add(3, 4)}");
    Console.WriteLine($"  mul(3,4) = {mul(3, 4)}");
}

static void DemoMulticast()
{
    Console.WriteLine("【2】多播委托");
    Action notify = () => Console.WriteLine("  订阅者 A 收到通知");
    notify += () => Console.WriteLine("  订阅者 B 收到通知");
    notify += () => Console.WriteLine("  订阅者 C 收到通知");
    Console.WriteLine("  调用多播委托：");
    notify();
}

static void DemoActionFunc()
{
    Console.WriteLine("【3】Action / Func");
    Action<string> greet = name => Console.WriteLine($"  你好，{name}！");
    Func<int, int, int> max = (a, b) => a > b ? a : b;
    greet("委托演示");
    Console.WriteLine($"  Func max(9, 4) = {max(9, 4)}");
}

static void DemoEvent()
{
    Console.WriteLine("【4】event");
    var publisher = new PricePublisher();
    publisher.PriceChanged += (_, e) =>
        Console.WriteLine($"  监听器1：价格变为 {e.NewPrice} 元");
    publisher.PriceChanged += (_, e) =>
        Console.WriteLine($"  监听器2：相对变化 {(e.NewPrice - e.OldPrice):+0;-0} 元");

    publisher.UpdatePrice(100);
    publisher.UpdatePrice(120);
}

delegate int IntOperation(int a, int b);

sealed class PriceChangedEventArgs(decimal oldPrice, decimal newPrice) : EventArgs
{
    public decimal OldPrice { get; } = oldPrice;
    public decimal NewPrice { get; } = newPrice;
}

sealed class PricePublisher
{
    private decimal _price;
    public event EventHandler<PriceChangedEventArgs>? PriceChanged;

    public void UpdatePrice(decimal newPrice)
    {
        var old = _price;
        _price = newPrice;
        PriceChanged?.Invoke(this, new PriceChangedEventArgs(old, newPrice));
    }
}
