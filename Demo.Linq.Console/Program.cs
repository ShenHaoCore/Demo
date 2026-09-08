Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== Demo.Linq.Console：LINQ 演示 ===\n");

var products = new[]
{
    new Product(1, "笔记本", "电子", 4999m),
    new Product(2, "鼠标", "电子", 99m),
    new Product(3, "键盘", "电子", 299m),
    new Product(4, "咖啡", "饮品", 28m),
    new Product(5, "茶叶", "饮品", 68m),
    new Product(6, "书桌", "家具", 899m)
};

var stocks = new[]
{
    new Stock(1, 12),
    new Stock(2, 100),
    new Stock(3, 45),
    new Stock(4, 200),
    new Stock(5, 80),
    new Stock(6, 8)
};

Console.WriteLine("【Where】价格大于 100 的商品：");
var expensive = products.Where(p => p.Price > 100);
foreach (var p in expensive)
    Console.WriteLine($"  {p.Name} - {p.Price} 元");

Console.WriteLine("\n【Select】投影为名称与分类：");
var names = products.Select(p => $"{p.Name}（{p.Category}）");
foreach (var n in names)
    Console.WriteLine($"  {n}");

Console.WriteLine("\n【GroupBy】按分类汇总：");
var groups = products.GroupBy(p => p.Category);
foreach (var g in groups)
{
    Console.WriteLine($"  分类 {g.Key}：共 {g.Count()} 件，均价 {g.Average(x => x.Price):F2} 元");
    foreach (var p in g)
        Console.WriteLine($"    - {p.Name}");
}

Console.WriteLine("\n【Join】商品与库存：");
var joined = products.Join(
    stocks,
    p => p.Id,
    s => s.ProductId,
    (p, s) => new { p.Name, p.Price, s.Quantity });
foreach (var row in joined)
    Console.WriteLine($"  {row.Name}：单价 {row.Price}，库存 {row.Quantity}");

Console.WriteLine("\n【延迟执行】查询在枚举时才真正执行：");
var source = new List<int> { 1, 2, 3 };
var query = source.Where(x =>
{
    Console.WriteLine($"  正在筛选 {x}");
    return x % 2 == 1;
});
Console.WriteLine("  （此时尚未执行 Where 谓词）");
source.Add(5);
Console.WriteLine("  源列表新增了 5，开始 foreach：");
foreach (var item in query)
    Console.WriteLine($"  得到奇数：{item}");
Console.WriteLine("  说明：延迟执行会看到枚举前对源集合的改动（5 也被筛选到）。");

Console.WriteLine("\n全部演示完成。");

record Product(int Id, string Name, string Category, decimal Price);
record Stock(int ProductId, int Quantity);
