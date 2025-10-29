# Day 2: Controller和路由深入

> **学习目标**: 掌握路由配置、处理各种HTTP请求、理解数据绑定和验证
>
> **预计时间**: 2-3小时
>
> **前置知识**: 完成Day 1的学习

---

## 📚 今日知识点

### 核心内容

1. 路由的多种配置方式
2. 处理查询参数（Query String）
3. 接收POST/PUT请求体（Request Body）
4. HTTP状态码的使用
5. 数据验证（Model Validation）

---

## 🚀 Step 1: 创建今天的项目

```bash
cd /Users/liqian/Desktop/Demo/2025-10/cursor-demo2

# 创建新项目
dotnet new webapi -n Day2RoutingAPI
cd Day2RoutingAPI

# 启动项目（带热重载）
dotnet watch run
```

---

## 🛣 Step 2: 路由的5种配置方式

创建 `Controllers/ProductController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Day2RoutingAPI.Controllers
{
    [ApiController]
    public class ProductController : ControllerBase
    {
        // 方式1: 控制器级路由 + 方法级路由
        [Route("api/products")]
        [HttpGet]
        public IActionResult GetAllProducts()
        {
            return Ok(new { message = "Get all products" });
        }

        // 方式2: 使用路由参数
        [Route("api/products/{id}")]
        [HttpGet]
        public IActionResult GetProductById(int id)
        {
            return Ok(new { id = id, name = $"Product {id}" });
        }

        // 方式3: 在HTTP特性中直接定义路由
        [HttpGet("api/products/featured")]
        public IActionResult GetFeaturedProducts()
        {
            return Ok(new { message = "Featured products" });
        }

        // 方式4: 使用 [action] 占位符
        [Route("api/products/[action]")]
        [HttpGet]
        public IActionResult Search()
        {
            return Ok(new { message = "Search products" });
        }

        // 方式5: 组合路由
        [HttpGet("api/products/category/{category}/page/{page}")]
        public IActionResult GetByCategory(string category, int page)
        {
            return Ok(new { 
                category = category, 
                page = page,
                message = $"Products in {category}, page {page}"
            });
        }
    }
}
```

**🔵 与Express.js对比:**

```javascript
// Express.js 等价写法
const router = express.Router();

// 方式1
router.get('/api/products', (req, res) => {
  res.json({ message: 'Get all products' });
});

// 方式2
router.get('/api/products/:id', (req, res) => {
  const id = req.params.id;
  res.json({ id, name: `Product ${id}` });
});

// 方式5
router.get('/api/products/category/:category/page/:page', (req, res) => {
  const { category, page } = req.params;
  res.json({ category, page });
});
```

**📝 关键点:**

- `{id}`, `{category}` - 路由参数（类似Express的 `:id`）
- 方法参数名要和路由参数名一致（自动绑定）
- `IActionResult` - 统一的返回类型（可以返回各种HTTP响应）
- `Ok()` - 返回200状态码（还有 `NotFound()`, `BadRequest()` 等）

**测试这些路由:**

```bash
curl http://localhost:5000/api/products
curl http://localhost:5000/api/products/123
curl http://localhost:5000/api/products/featured
curl http://localhost:5000/api/products/search
curl http://localhost:5000/api/products/category/electronics/page/2
```

---

## 🔍 Step 3: 处理查询参数（Query Parameters）

查询参数就是URL后面的 `?key=value&key2=value2`

创建 `Controllers/SearchController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Day2RoutingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        // 单个查询参数
        // GET /api/search?keyword=laptop
        [HttpGet]
        public IActionResult Search([FromQuery] string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return BadRequest(new { error = "Keyword is required" });
            }
            
            return Ok(new { 
                keyword = keyword,
                results = $"Found results for: {keyword}"
            });
        }

        // 多个查询参数
        // GET /api/search/advanced?keyword=laptop&minPrice=500&maxPrice=2000&sort=price
        [HttpGet("advanced")]
        public IActionResult AdvancedSearch(
            [FromQuery] string keyword,
            [FromQuery] decimal? minPrice,    // ? 表示可选参数
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? sort = "name" // 默认值
        )
        {
            var query = new
            {
                keyword = keyword,
                minPrice = minPrice ?? 0,      // ?? 是空合并运算符（类似JS的 ||）
                maxPrice = maxPrice ?? 999999,
                sort = sort
            };

            return Ok(new {
                query = query,
                message = "Advanced search executed"
            });
        }

        // 使用对象接收查询参数
        [HttpGet("filter")]
        public IActionResult FilterProducts([FromQuery] ProductFilter filter)
        {
            return Ok(new {
                filter = filter,
                message = "Products filtered"
            });
        }
    }

    // 查询参数模型
    public class ProductFilter
    {
        public string? Keyword { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Category { get; set; }
        public bool InStock { get; set; } = true;  // 默认值
    }
}
```

**🔵 与Express.js对比:**

```javascript
// Express.js
app.get('/api/search', (req, res) => {
  const { keyword, minPrice, maxPrice, sort } = req.query;
  
  if (!keyword) {
    return res.status(400).json({ error: 'Keyword is required' });
  }
  
  res.json({ keyword, minPrice, maxPrice, sort });
});
```

**📝 关键点:**

- `[FromQuery]` - 告诉框架从查询字符串获取参数
- `string?` - 可空类型（可以不传这个参数）
- `decimal?` - 数字也可以是可空的
- `?? 运算符` - 空合并运算符，类似JS的 `||` 或 `??`
- 可以用对象接收所有查询参数（更清晰）

**测试:**

```bash
curl "http://localhost:5000/api/search?keyword=laptop"
curl "http://localhost:5000/api/search/advanced?keyword=phone&minPrice=500&maxPrice=1500&sort=price"
curl "http://localhost:5000/api/search/filter?keyword=book&minPrice=10&category=education&inStock=true"
```

---

## 📮 Step 4: 处理POST请求（Request Body）

POST请求通常带有请求体（body），用于创建或更新数据。

创建 `Controllers/UserController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Day2RoutingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        // 模拟数据库
        private static List<User> _users = new List<User>
        {
            new User { Id = 1, Name = "张三", Email = "zhang@example.com", Age = 25 },
            new User { Id = 2, Name = "李四", Email = "li@example.com", Age = 30 }
        };

        // GET: 获取所有用户
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            return Ok(_users);
        }

        // GET: 获取单个用户
        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            
            if (user == null)
            {
                return NotFound(new { error = $"User with id {id} not found" });
            }
            
            return Ok(user);
        }

        // POST: 创建用户
        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto dto)
        {
            // 模型验证（自动进行）
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);  // 返回验证错误
            }

            var newUser = new User
            {
                Id = _users.Max(u => u.Id) + 1,  // 生成新ID
                Name = dto.Name,
                Email = dto.Email,
                Age = dto.Age
            };

            _users.Add(newUser);

            // CreatedAtAction: 返回201状态码和新资源的位置
            return CreatedAtAction(
                nameof(GetUserById),     // 指向获取资源的方法
                new { id = newUser.Id }, // 路由参数
                newUser                  // 返回的数据
            );
        }

        // PUT: 更新用户
        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            
            if (user == null)
            {
                return NotFound(new { error = $"User with id {id} not found" });
            }

            // 更新字段
            if (!string.IsNullOrEmpty(dto.Name))
                user.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;
            if (dto.Age.HasValue)
                user.Age = dto.Age.Value;

            return Ok(user);
        }

        // DELETE: 删除用户
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            
            if (user == null)
            {
                return NotFound(new { error = $"User with id {id} not found" });
            }

            _users.Remove(user);
            
            return NoContent();  // 204 - 成功删除，无内容返回
        }
    }

    // 用户模型
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    // 创建用户DTO（Data Transfer Object）
    public class CreateUserDto
    {
        [Required(ErrorMessage = "姓名不能为空")]
        [MinLength(2, ErrorMessage = "姓名至少2个字符")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "邮箱不能为空")]
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        public string Email { get; set; } = string.Empty;

        [Range(1, 120, ErrorMessage = "年龄必须在1-120之间")]
        public int Age { get; set; }
    }

    // 更新用户DTO
    public class UpdateUserDto
    {
        [MinLength(2, ErrorMessage = "姓名至少2个字符")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        public string? Email { get; set; }

        [Range(1, 120, ErrorMessage = "年龄必须在1-120之间")]
        public int? Age { get; set; }
    }
}
```

**🔵 与Express.js对比:**

```javascript
// Express.js
const users = [
  { id: 1, name: '张三', email: 'zhang@example.com', age: 25 }
];

// GET all
app.get('/api/user', (req, res) => {
  res.json(users);
});

// GET by id
app.get('/api/user/:id', (req, res) => {
  const user = users.find(u => u.id == req.params.id);
  if (!user) {
    return res.status(404).json({ error: 'User not found' });
  }
  res.json(user);
});

// POST create
app.post('/api/user', (req, res) => {
  const { name, email, age } = req.body;
  
  // 手动验证
  if (!name || !email) {
    return res.status(400).json({ error: 'Name and email required' });
  }
  
  const newUser = {
    id: users.length + 1,
    name, email, age
  };
  users.push(newUser);
  
  res.status(201).json(newUser);
});

// PUT update
app.put('/api/user/:id', (req, res) => {
  const user = users.find(u => u.id == req.params.id);
  if (!user) {
    return res.status(404).json({ error: 'User not found' });
  }
  
  Object.assign(user, req.body);
  res.json(user);
});

// DELETE
app.delete('/api/user/:id', (req, res) => {
  const index = users.findIndex(u => u.id == req.params.id);
  if (index === -1) {
    return res.status(404).json({ error: 'User not found' });
  }
  
  users.splice(index, 1);
  res.status(204).send();
});
```

**📝 关键点:**

1. **`[FromBody]`** - 从请求体获取数据（JSON格式）

2. **DTO（Data Transfer Object）**
    - 用于接收和返回数据的对象
    - 与数据库模型分离（更安全，更灵活）
    - 类似前端的interface，但带验证规则

3. **数据验证特性**
    - `[Required]` - 必填（类似前端的 required）
    - `[EmailAddress]` - 邮箱格式
    - `[Range(min, max)]` - 数值范围
    - `[MinLength]` / `[MaxLength]` - 字符串长度
    - 自动验证，失败返回400错误

4. **HTTP状态码方法**
   ```csharp
   Ok(data)              // 200 - 成功
   Created()             // 201 - 创建成功
   CreatedAtAction()     // 201 - 创建成功，返回资源位置
   NoContent()           // 204 - 成功，无内容
   BadRequest()          // 400 - 请求错误
   NotFound()            // 404 - 未找到
   ```

5. **LINQ方法**（类似JS数组方法）
   ```csharp
   // C#                          // JavaScript
   .FirstOrDefault(u => u.Id==1)  // .find(u => u.id === 1)
   .Where(u => u.Age > 18)        // .filter(u => u.age > 18)
   .Select(u => u.Name)           // .map(u => u.name)
   .Any(u => u.Age > 18)          // .some(u => u.age > 18)
   .Max(u => u.Id)                // Math.max(...users.map(u => u.id))
   ```

---

## 🧪 Step 5: 测试完整的CRUD

### 使用curl测试

```bash
# 1. 获取所有用户
curl http://localhost:5000/api/user

# 2. 获取单个用户
curl http://localhost:5000/api/user/1

# 3. 创建用户（成功）
curl -X POST http://localhost:5000/api/user \
  -H "Content-Type: application/json" \
  -d '{"name":"王五","email":"wang@example.com","age":28}'

# 4. 创建用户（验证失败 - 邮箱格式错误）
curl -X POST http://localhost:5000/api/user \
  -H "Content-Type: application/json" \
  -d '{"name":"赵六","email":"invalid-email","age":25}'

# 5. 更新用户
curl -X PUT http://localhost:5000/api/user/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"张三三","age":26}'

# 6. 删除用户
curl -X DELETE http://localhost:5000/api/user/2
```

### 使用Swagger测试

1. 访问 `https://localhost:5001/swagger`
2. 展开各个端点
3. 点击 "Try it out"
4. 输入参数或请求体
5. 点击 "Execute"
6. 查看响应

---

## 🎨 Step 6: 创建一个简单的前端页面

在项目根目录创建 `wwwroot/index.html`：

```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>用户管理</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
        }
        .user-card {
            border: 1px solid #ddd;
            padding: 15px;
            margin: 10px 0;
            border-radius: 5px;
        }
        input, button {
            padding: 8px;
            margin: 5px;
        }
        button {
            background: #007bff;
            color: white;
            border: none;
            cursor: pointer;
            border-radius: 3px;
        }
        button:hover {
            background: #0056b3;
        }
        .error {
            color: red;
        }
    </style>
</head>
<body>
    <h1>用户管理系统</h1>
    
    <div>
        <h2>创建用户</h2>
        <input type="text" id="name" placeholder="姓名">
        <input type="email" id="email" placeholder="邮箱">
        <input type="number" id="age" placeholder="年龄">
        <button onclick="createUser()">创建</button>
        <div id="error" class="error"></div>
    </div>

    <div>
        <h2>用户列表</h2>
        <button onclick="loadUsers()">刷新</button>
        <div id="users"></div>
    </div>

    <script>
        const API_URL = 'http://localhost:5000/api/user';

        // 加载用户列表
        async function loadUsers() {
            try {
                const response = await fetch(API_URL);
                const users = await response.json();
                
                const usersDiv = document.getElementById('users');
                usersDiv.innerHTML = users.map(user => `
                    <div class="user-card">
                        <strong>${user.name}</strong> (${user.age}岁)
                        <br>📧 ${user.email}
                        <br>
                        <button onclick="deleteUser(${user.id})">删除</button>
                    </div>
                `).join('');
            } catch (error) {
                console.error('Error:', error);
            }
        }

        // 创建用户
        async function createUser() {
            const name = document.getElementById('name').value;
            const email = document.getElementById('email').value;
            const age = parseInt(document.getElementById('age').value);
            
            try {
                const response = await fetch(API_URL, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ name, email, age })
                });

                if (!response.ok) {
                    const error = await response.json();
                    document.getElementById('error').textContent = 
                        JSON.stringify(error, null, 2);
                    return;
                }

                document.getElementById('error').textContent = '';
                document.getElementById('name').value = '';
                document.getElementById('email').value = '';
                document.getElementById('age').value = '';
                
                loadUsers();
            } catch (error) {
                console.error('Error:', error);
            }
        }

        // 删除用户
        async function deleteUser(id) {
            if (!confirm('确定删除？')) return;
            
            try {
                await fetch(`${API_URL}/${id}`, {
                    method: 'DELETE'
                });
                loadUsers();
            } catch (error) {
                console.error('Error:', error);
            }
        }

        // 页面加载时获取用户列表
        loadUsers();
    </script>
</body>
</html>
```

**启用静态文件支持**，在 `Program.cs` 中添加：

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加CORS支持（允许前端跨域访问）
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 启用静态文件
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseCors();  // 启用CORS
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**访问**: `http://localhost:5000/index.html`

---

## 📝 今日总结

### ✅ 你学会了：

- [x] 5种路由配置方式
- [x] 处理查询参数（FromQuery）
- [x] 处理请求体（FromBody）
- [x] 完整的CRUD操作
- [x] 数据验证（特性标注）
- [x] HTTP状态码的使用
- [x] LINQ查询方法
- [x] 前后端集成（CORS）

### 🔑 关键对比：

| 功能   | Express.js        | ASP.NET Core        |
|------|-------------------|---------------------|
| 路由参数 | `:id`             | `{id}`              |
| 查询参数 | `req.query`       | `[FromQuery]`       |
| 请求体  | `req.body`        | `[FromBody]`        |
| 数组过滤 | `.filter()`       | `.Where()`          |
| 数组查找 | `.find()`         | `.FirstOrDefault()` |
| 数组映射 | `.map()`          | `.Select()`         |
| 状态码  | `res.status(404)` | `NotFound()`        |

---

## 🎯 明日预告：Day 3 - 数据模型和业务逻辑

明天你将学习：

- 如何组织更复杂的数据模型
- 关系型数据（一对多、多对多）
- 业务逻辑层（Service Layer）
- 依赖注入（DI）的深入理解

---

## 💾 作业

1. 完成上面的 `UserController` 和前端页面
2. 扩展功能：添加用户搜索功能（按姓名或邮箱搜索）
3. 添加分页功能：`GET /api/user?page=1&pageSize=10`
4. 思考：为什么要用DTO而不是直接用Model？

**提示作业代码框架：**

```csharp
// 分页查询
[HttpGet("paged")]
public IActionResult GetUsersPaged(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    var skip = (page - 1) * pageSize;
    var users = _users.Skip(skip).Take(pageSize);
    
    return Ok(new {
        page = page,
        pageSize = pageSize,
        total = _users.Count,
        data = users
    });
}

// 搜索用户
[HttpGet("search")]
public IActionResult SearchUsers([FromQuery] string keyword)
{
    var results = _users.Where(u => 
        u.Name.Contains(keyword) || 
        u.Email.Contains(keyword)
    );
    
    return Ok(results);
}
```

---

**继续加油！🚀**


