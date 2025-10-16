# Day 1: 环境搭建 + Hello World API

> **学习目标**: 搭建开发环境，创建第一个ASP.NET Core Web API项目，理解项目结构
> 
> **预计时间**: 2-3小时
> 
> **前置知识**: 了解HTTP协议、RESTful API概念（作为前端开发者你已经具备）

---

## 📚 今日知识点

### 1. .NET 和 ASP.NET Core 是什么？

**🔵 前端对比理解:**
- **.NET SDK** ≈ Node.js（运行环境）
- **ASP.NET Core** ≈ Express.js（Web框架）
- **NuGet** ≈ npm（包管理器）
- **dotnet CLI** ≈ npm/npx（命令行工具）

| 前端 | C#后端 | 说明 |
|------|--------|------|
| `node -v` | `dotnet --version` | 查看版本 |
| `npm install` | `dotnet restore` | 安装依赖 |
| `npm start` | `dotnet run` | 运行项目 |
| `npm init` | `dotnet new` | 创建项目 |
| `package.json` | `.csproj` | 项目配置文件 |

---

## 🛠 Step 1: 安装 .NET SDK

### MacOS 安装步骤

1. **下载安装包**
   - 访问: https://dotnet.microsoft.com/download
   - 下载 **.NET 8 SDK** (选择MacOS版本)

2. **验证安装**
   打开终端，运行：
   ```bash
   dotnet --version
   ```
   
   应该看到类似输出：
   ```
   8.0.xxx
   ```

3. **查看所有已安装的SDK**
   ```bash
   dotnet --list-sdks
   ```

**💡 类比**: 就像安装Node.js后可以用 `node -v` 查看版本一样。

---

## 🚀 Step 2: 创建第一个Web API项目

### 2.1 创建项目

在终端中运行以下命令：

```bash
# 1. 进入你的工作目录
cd /Users/liqian/Desktop/Demo/2025-10/cursor-demo2

# 2. 创建Web API项目
dotnet new webapi -n Day1HelloAPI

# 3. 进入项目目录
cd Day1HelloAPI

# 4. 运行项目
dotnet run
```

**🔍 命令解析:**
- `dotnet new webapi` - 创建Web API项目模板（类似 `npx create-react-app`）
- `-n Day1HelloAPI` - 项目名称（name）
- `dotnet run` - 编译并运行项目（类似 `npm start`）

### 2.2 首次运行

运行后你会看到类似输出：
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**✅ 成功标志**: 
- 项目在 `http://localhost:5000` 和 `https://localhost:5001` 运行
- 打开浏览器访问: `https://localhost:5001/swagger`
- 你会看到一个漂亮的API文档界面（Swagger UI）

**💡 前端对比**: 
- 类似React运行在 `localhost:3000`
- Swagger相当于自动生成的API文档（类似Postman的Collections）

---

## 📂 Step 3: 理解项目结构

打开项目文件夹，你会看到这样的结构：

```
Day1HelloAPI/
├── Controllers/                 # 📁 API控制器目录
│   └── WeatherForecastController.cs
├── Properties/
│   └── launchSettings.json     # 启动配置（端口、环境等）
├── appsettings.json            # 应用配置文件
├── appsettings.Development.json
├── Day1HelloAPI.csproj         # 项目文件（类似package.json）
├── Program.cs                  # 🔥 程序入口（最重要！）
└── WeatherForecast.cs          # 数据模型
```

### 📄 重要文件详解

#### 3.1 `Program.cs` - 程序入口

**前端对比**: 相当于你的 `index.js` 或 `main.js`

打开 `Program.cs`，你会看到：

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**🔵 与Express.js对比理解:**

```javascript
// Express.js 写法
const express = require('express');
const app = express();

app.use(express.json());           // ← 类似 builder.Services.AddControllers()
app.use('/api', routes);           // ← 类似 app.MapControllers()

app.listen(3000, () => {           // ← 类似 app.Run()
  console.log('Server running');
});
```

**逐行解释:**
- `var builder = WebApplication.CreateBuilder(args);` 
  - 创建应用构建器（准备配置你的应用）

- `builder.Services.AddControllers();`
  - 添加控制器支持（告诉应用我们要用Controller处理请求）

- `builder.Services.AddSwaggerGen();`
  - 添加Swagger支持（自动生成API文档）

- `var app = builder.Build();`
  - 构建应用（完成配置，创建应用实例）

- `app.UseSwagger();` 和 `app.UseSwaggerUI();`
  - 启用Swagger（开发环境才开启）

- `app.MapControllers();`
  - 映射控制器路由（让API请求能找到对应的Controller）

- `app.Run();`
  - 运行应用（开始监听HTTP请求）

---

#### 3.2 `WeatherForecastController.cs` - 控制器

**前端对比**: 相当于Express的路由处理函数

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Day1HelloAPI.Controllers
{
    [ApiController]                          // ① 标记这是API控制器
    [Route("[controller]")]                   // ② 定义路由
    public class WeatherForecastController : ControllerBase
    {
        [HttpGet(Name = "GetWeatherForecast")] // ③ HTTP GET方法
        public IEnumerable<WeatherForecast> Get()
        {
            // 返回数据
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
```

**🔵 与Express.js对比:**

```javascript
// Express.js 写法
app.get('/weatherforecast', (req, res) => {  // ← 类似 [HttpGet]
  const data = [/* ... */];
  res.json(data);                             // ← C#自动序列化为JSON
});
```

**关键概念解释:**

1. **`[ApiController]`** - 特性(Attribute)
   - 前端没有直接对应概念，类似装饰器(Decorator)
   - 标记这个类是API控制器，提供自动验证等功能

2. **`[Route("[controller]")]`** - 路由配置
   - `[controller]` 会被替换为控制器名称（去掉"Controller"后缀）
   - 所以 `WeatherForecastController` → 路由为 `/weatherforecast`

3. **`[HttpGet]`** - HTTP方法特性
   - 表示这个方法处理GET请求
   - 还有 `[HttpPost]`, `[HttpPut]`, `[HttpDelete]` 等

4. **`ControllerBase`** - 基类
   - 继承它获得很多便利方法（如 `Ok()`, `NotFound()` 等）

---

#### 3.3 `WeatherForecast.cs` - 数据模型

**前端对比**: 相当于TypeScript的interface或type

```csharp
namespace Day1HelloAPI
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }      // 日期
        public int TemperatureC { get; set; }   // 温度（摄氏度）
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556); // 计算属性
        public string? Summary { get; set; }    // 描述（?表示可为null）
    }
}
```

**🔵 与TypeScript对比:**

```typescript
// TypeScript 写法
interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;          // C#中是计算属性
  summary: string | null;        // C#中用 string? 表示
}
```

**C# vs JavaScript 类型对比:**

| C# | JavaScript/TypeScript | 说明 |
|----|----------------------|------|
| `int` | `number` | 整数 |
| `string` | `string` | 字符串 |
| `bool` | `boolean` | 布尔值 |
| `string?` | `string \| null` | 可空类型 |
| `DateOnly` | `Date` | 日期 |
| `List<T>` | `Array<T>` | 数组/列表 |

---

## 🎯 Step 4: 测试你的第一个API

### 4.1 使用Swagger测试

1. 确保项目正在运行（`dotnet run`）
2. 打开浏览器访问: `https://localhost:5001/swagger`
3. 你会看到 **"GET /WeatherForecast"** 接口
4. 点击 **"Try it out"** → **"Execute"**
5. 查看返回结果（JSON格式）

### 4.2 使用curl测试

在另一个终端运行：
```bash
curl http://localhost:5000/weatherforecast
```

你会看到JSON响应：
```json
[
  {
    "date": "2025-10-16",
    "temperatureC": 25,
    "temperatureF": 76,
    "summary": "Warm"
  },
  ...
]
```

---

## ✏️ Step 5: 动手练习 - 修改代码

现在你要修改代码，加深理解！

### 练习1: 修改路由

**任务**: 将路由从 `/weatherforecast` 改为 `/api/weather`

**步骤**:
1. 打开 `WeatherForecastController.cs`
2. 找到 `[Route("[controller]")]`
3. 改为 `[Route("api/weather")]`
4. 保存文件
5. 重启项目（Ctrl+C停止，再 `dotnet run`）
6. 访问: `http://localhost:5000/api/weather`

**✅ 期望结果**: 原来的路由404，新路由返回数据

---

### 练习2: 添加新的API方法

**任务**: 添加一个返回单个天气数据的API

**步骤**: 在 `WeatherForecastController.cs` 中添加新方法：

```csharp
[HttpGet("{id}")]  // 路由参数（类似 Express 的 :id）
public WeatherForecast GetById(int id)
{
    // 生成单个数据
    return new WeatherForecast
    {
        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(id)),
        TemperatureC = Random.Shared.Next(-20, 55),
        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    };
}
```

**🔵 前端对比:**
```javascript
// Express.js
app.get('/api/weather/:id', (req, res) => {
  const id = req.params.id;
  res.json(/* ... */);
});
```

**测试**: 
- 访问 `http://localhost:5000/api/weather/1`
- 访问 `http://localhost:5000/api/weather/5`

---

### 练习3: 创建自己的Controller

**任务**: 创建一个 `HelloController`，返回欢迎消息

**步骤**:

1. 在 `Controllers` 文件夹下创建 `HelloController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Day1HelloAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelloController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return "Hello from C# Backend!";
        }
        
        [HttpGet("{name}")]
        public string GetByName(string name)
        {
            return $"Hello, {name}! Welcome to C# world!";
        }
    }
}
```

**解释**:
- `$"Hello, {name}!"` - C#的字符串插值（类似JS的模板字符串 `Hello, ${name}!`）
- `{name}` - 路由参数，自动绑定到方法参数

2. 重启项目

**测试**:
- `http://localhost:5000/api/hello` → "Hello from C# Backend!"
- `http://localhost:5000/api/hello/张三` → "Hello, 张三! Welcome to C# world!"

---

## 🔧 常见问题解答

### Q1: 修改代码后需要重启吗？
**A**: 默认需要。但可以启用热重载：
```bash
dotnet watch run  # 文件改动自动重启（类似nodemon）
```

### Q2: 端口被占用怎么办？
**A**: 修改 `Properties/launchSettings.json`：
```json
"applicationUrl": "https://localhost:7001;http://localhost:5001"
```

### Q3: Swagger访问不了？
**A**: 检查是否在开发环境：
```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

### Q4: 为什么C#需要编译？
**A**: 
- JavaScript是解释型语言（运行时解释）
- C#是编译型语言（先编译成IL，再JIT执行）
- 优点：运行速度快，类型安全
- `dotnet run` 会自动编译

---

## 📝 今日总结

### ✅ 你学会了：
- [x] 安装.NET SDK
- [x] 创建ASP.NET Core Web API项目
- [x] 理解项目结构（Program.cs, Controller, Model）
- [x] 使用Swagger测试API
- [x] 创建自己的Controller和API端点
- [x] 理解路由、HTTP方法、数据模型

### 🔑 关键概念（前端对比）:
| C#概念 | 前端对应 | 说明 |
|--------|----------|------|
| `Program.cs` | `index.js` | 程序入口 |
| `Controller` | Express路由 | 处理HTTP请求 |
| `Model` | TypeScript Interface | 数据结构 |
| `[HttpGet]` | `app.get()` | GET请求处理 |
| `dotnet run` | `npm start` | 运行项目 |
| `.csproj` | `package.json` | 项目配置 |

---

## 🎯 明日预告：Day 2 - Controller和路由深入

明天你将学习：
- 路由的各种配置方式
- 如何处理查询参数（query string）
- 如何接收POST请求体（body）
- HTTP状态码的返回
- 数据验证

---

## 💾 作业

完成以上所有练习，并尝试：
1. 创建一个 `StudentController`，包含：
   - `GET /api/student` - 返回学生列表
   - `GET /api/student/{id}` - 返回单个学生
   - 学生模型包含：Id, Name, Age, Grade

2. 用你熟悉的前端技术（React/Vue/原生JS）写一个页面调用这个API

3. 记录你遇到的问题和思考

---

**恭喜完成Day 1！🎉**

休息一下，明天继续加油！


