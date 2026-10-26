# Demo.Delegates.Console

对应笔记：**CSharp / 委托** · 控制台程序，直接 `dotnet run` 观察输出

从自定义 delegate 到 event 的四步递进：自定义委托类型、多播委托、`Action`/`Func`、`event` 发布订阅。

## 核心机制

- 自定义 `delegate int IntOperation(int a, int b)`：委托是类型，lambda 是实例；`add` / `mul` 两个实例签名相同、行为不同。
- **多播委托**：`notify += A; notify += B; notify += C;`，一次调用按加入顺序依次执行所有订阅者；返回值会被丢弃，所以多播场景用 `Action` 最合适。
- `Action<T>` / `Func<T,TResult>` 是框架内置泛型委托，绝大多数场景不必再手写 delegate 类型——自定义 delegate 仅在需要语义命名（如 `IntOperation`）时才有价值。
- `event` 是对委托字段的封装：外部只能 `+=` / `-=`，`Invoke` 只留在发布者内部（`PriceChanged?.Invoke(this, args)`），这正是"发布者控制触发、订阅者控制注册"的边界；`?.` 保证无订阅者时触发不会抛 `NullReferenceException`。
- 事件数据继承 `EventArgs`：`PriceChangedEventArgs` 携带 `OldPrice` / `NewPrice`，两个监听器分别演示"读新值"与"计算相对变化"。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Program.cs](Program.cs) | 四个演示场景、`IntOperation` 委托与 `PricePublisher` 发布者，单文件实现 |

## 运行

```bash
cd Demo.Delegates.Console
dotnet run
```

无 HTTP 接口；运行后按【1】到【4】观察自定义委托调用、多播执行顺序、Action/Func 用法与事件触发。

## 输出观察点

- 【1】同一签名的两个 lambda 实例分别求值：`add(3,4)=7`、`mul(3,4)=12`。
- 【2】"订阅者 A/B/C"严格按 `+=` 的加入顺序打印，说明多播调用是顺序执行。
- 【4】`UpdatePrice(100)` 触发两个监听器；`UpdatePrice(120)` 时监听器 2 用 `+0;-0` 格式输出相对变化 `+20`。

## 笔记要点

- 委托是"方法的类型化引用"，多播委托把一串方法变成一次按序调用。
- `event` 的本质是访问器封装：禁止外部 `Invoke` 与整体赋值，只暴露 `+=` / `-=` 订阅口。
- `notify += B` 是"创建新委托链再赋回"的不可变语义，并不是原地往旧实例里追加。
