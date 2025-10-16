# C# 后端开发 - 14天从零到MQTT/Modbus IoT项目

> 专为前端开发者设计的C#后端学习路径
> 
> 从基础Web API到完整工业物联网网关系统

---

## 📚 课程简介

这是一套为期14天的C#后端开发学习课程，专门为有前端基础、了解C#基本语法、想学习后端开发的开发者设计。

**特色：**
- ✅ 每天2-3小时，循序渐进
- ✅ 大量前端概念对比（React/Vue/JavaScript/TypeScript）
- ✅ 实战导向，理论与实践结合
- ✅ 最终完成MQTT+Modbus工业IoT项目

---

## 🎯 学习目标

完成本课程后，你将能够：
- 独立开发ASP.NET Core Web API项目
- 掌握数据库操作（Entity Framework Core）
- 理解并应用分层架构设计
- 实现MQTT和Modbus协议通信
- 构建完整的IoT网关系统

---

## 📅 课程大纲

### 第一周：ASP.NET Core基础

| 天数 | 主题 | 核心内容 | 文档 |
|-----|------|---------|------|
| **Day 1** | 环境搭建和Hello World | .NET SDK安装、创建第一个API、Swagger | [📄 Day1-环境搭建和HelloWorld.md](./Day1-环境搭建和HelloWorld.md) |
| **Day 2** | Controller和路由深入 | 路由配置、查询参数、POST/PUT/DELETE | [📄 Day2-Controller和路由深入.md](./Day2-Controller和路由深入.md) |
| **Day 3** | 数据模型和内存存储 | 数据模型设计、DTO模式、CRUD完整实现 | [📄 Day3-数据模型和内存存储.md](./Day3-数据模型和内存存储.md) |
| **Day 4** | Entity Framework与数据库 | EF Core、SQLite、数据库迁移 | [📄 Day4-EntityFramework与数据库.md](./Day4-EntityFramework与数据库.md) |
| **Day 5** | 服务层和依赖注入 | 分层架构、Repository模式、DI容器 | [📄 Day5-服务层和依赖注入.md](./Day5-服务层和依赖注入.md) |
| **Day 6** | 异步编程和LINQ深入 | async/await、Task、LINQ 30个方法 | [📄 Day6-异步编程和LINQ深入.md](./Day6-异步编程和LINQ深入.md) |
| **Day 7** | 中间件和异常处理 | 中间件管道、全局异常处理、日志系统 | [📄 Day7-中间件和异常处理.md](./Day7-中间件和异常处理.md) |

### 第二周：物联网协议实战

| 天数 | 主题 | 核心内容 | 文档 |
|-----|------|---------|------|
| **Day 8** | MQTT协议入门 | 发布/订阅模式、QoS、主题设计 | [📄 Day8-MQTT协议入门.md](./Day8-MQTT协议入门.md) |
| **Day 9** | C#实现MQTT客户端 | MQTTnet库、发布订阅、前端集成 | [📄 Day9-C#实现MQTT客户端.md](./Day9-C#实现MQTT客户端.md) |
| **Day 10** | MQTT消息持久化 | 数据存储、历史查询、数据聚合 | [📄 Day10-MQTT消息持久化.md](./Day10-MQTT消息持久化.md) |
| **Day 11** | Modbus协议入门 | 主从架构、寄存器类型、数据解析 | [📄 Day11-Modbus协议入门.md](./Day11-Modbus协议入门.md) |
| **Day 12** | C#实现Modbus客户端 | NModbus库、寄存器读写、轮询服务 | [📄 Day12-C#实现Modbus客户端.md](./Day12-C#实现Modbus客户端.md) |
| **Day 13** | MQTT和Modbus整合 | IoT网关、协议转换、实时监控 | [📄 Day13-MQTT和Modbus整合项目.md](./Day13-MQTT和Modbus整合项目.md) |
| **Day 14** | 项目总结与进阶方向 | 性能优化、部署、学习路线 | [📄 Day14-项目总结与进阶方向.md](./Day14-项目总结与进阶方向.md) |

---

## 🔑 核心技术栈

```
┌─────────────────────────────────────────────────────┐
│                   技术架构                           │
└─────────────────────────────────────────────────────┘

Web框架:       ASP.NET Core 8.0
数据库:         SQLite → Entity Framework Core
IoT协议:        MQTT (MQTTnet) + Modbus (NModbus4)
实时通信:       MQTT Broker (Mosquitto/EMQX)
前端:          HTML/JavaScript (MQTT.js)
部署:          Docker / Linux systemd
```

---

## 🚀 快速开始

### 前置要求

- macOS / Windows / Linux
- 了解HTTP、RESTful API基本概念
- 熟悉前端开发（React/Vue/JavaScript）
- 掌握C#基础语法（变量、循环、类等）

### 环境准备

1. **安装.NET SDK 8.0**
   ```bash
   # macOS
   brew install dotnet
   
   # 验证安装
   dotnet --version
   ```

2. **安装开发工具**
   - Visual Studio Code + C# 扩展
   - 或 Visual Studio 2022

3. **安装数据库工具**（可选）
   - SQLite Browser（查看数据库）

4. **安装MQTT Broker**（Day 8开始需要）
   ```bash
   # 使用Docker
   docker run -d -p 1883:1883 eclipse-mosquitto
   
   # 或使用在线Broker
   # broker.emqx.io:1883
   ```

---

## 📖 使用指南

### 学习方式

1. **按顺序学习** - 每天完成一个主题
2. **动手实践** - 跟着文档敲代码，不要复制粘贴
3. **完成作业** - 每天的作业帮助巩固知识
4. **理解对比** - 利用前端知识对比理解C#概念

### 文档结构

每天的文档包含：
- 📚 **知识点讲解** - 核心概念
- 🔵 **前端对比** - 与JavaScript/TypeScript对比
- 🚀 **实战步骤** - 手把手教学
- 💡 **最佳实践** - 工程化经验
- 💾 **作业练习** - 巩固知识

---

## 🎯 学习检查清单

### Week 1 - 基础能力

- [ ] 能够创建和运行ASP.NET Core项目
- [ ] 理解Controller、Service、Repository分层
- [ ] 掌握路由配置和HTTP方法
- [ ] 会使用Entity Framework Core操作数据库
- [ ] 理解依赖注入（DI）
- [ ] 能够编写异步代码（async/await）
- [ ] 掌握LINQ查询方法

### Week 2 - IoT实战

- [ ] 理解MQTT发布/订阅模式
- [ ] 能够实现MQTT客户端
- [ ] 理解Modbus协议和寄存器
- [ ] 能够读写Modbus设备
- [ ] 完成MQTT和Modbus的整合
- [ ] 实现完整的IoT网关项目

---

## 💡 学习技巧

### 1. 利用前端知识类比

```javascript
// JavaScript/TypeScript 你熟悉的
const users = await fetch('/api/users').then(r => r.json());
const admins = users.filter(u => u.role === 'admin');

// C# 你要学的
var users = await _context.Users.ToListAsync();
var admins = users.Where(u => u.Role == "admin");
```

### 2. 理解对应关系

| 前端概念 | C#后端概念 |
|---------|-----------|
| Express路由 | ASP.NET Controller |
| axios/fetch | HttpClient |
| Prisma/TypeORM | Entity Framework Core |
| npm | NuGet |
| package.json | .csproj |
| async/await | async/await（语法相同） |
| Promise | Task |

### 3. 实践为主

不要只看不练，每个示例代码都要自己敲一遍，运行看效果。

---

## 🏆 项目成果展示

完成14天学习后，你将拥有一个完整的工业IoT网关项目：

```
你的IoT网关系统
├── ✅ Modbus设备数据采集
├── ✅ MQTT消息发布订阅
├── ✅ 实时数据存储到数据库
├── ✅ RESTful API查询接口
├── ✅ 实时监控Web面板
├── ✅ 设备管理功能
├── ✅ 数据统计和分析
└── ✅ 错误处理和日志
```

---

## 📚 补充资源

### 官方文档
- [Microsoft Learn - ASP.NET Core](https://learn.microsoft.com/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [C# Language Reference](https://learn.microsoft.com/dotnet/csharp/)

### 工具下载
- [.NET SDK](https://dotnet.microsoft.com/download)
- [Visual Studio Code](https://code.visualstudio.com/)
- [MQTTX](https://mqttx.app/) - MQTT测试工具
- [SQLite Browser](https://sqlitebrowser.org/)

### 在线资源
- [MQTT.org](https://mqtt.org/) - MQTT官方网站
- [Modbus.org](https://modbus.org/) - Modbus官方网站

---

## ❓ 常见问题

**Q: 我完全没有C#基础可以学吗？**
A: 只要你掌握C#基本语法（变量、循环、类、继承），就可以开始。课程会从零开始教你后端开发。

**Q: 每天学习需要多长时间？**
A: 建议每天投入2-3小时，包括阅读文档、编写代码和完成练习。

**Q: 学完能找到工作吗？**
A: 这套课程让你掌握C#后端基础和IoT项目经验，但找工作还需要更多实践和项目经验。建议继续深入学习。

**Q: 为什么选择MQTT和Modbus？**
A: 这两个协议在工业物联网中应用广泛，学会它们能让你有实际的项目经验，而不是简单的CRUD。

**Q: 遇到问题怎么办？**
A: 
1. 查看错误信息（C#的错误提示很详细）
2. Google搜索错误信息
3. Stack Overflow查找解决方案
4. 查阅官方文档

---

## 🤝 贡献

如果你在学习过程中发现错误或有改进建议，欢迎：
- 提交Issue
- 发起Pull Request
- 分享你的学习心得

---

## 📜 许可

本教程采用 MIT License，可以自由使用和分享。

---

## ✨ 致谢

感谢所有为.NET、MQTT、Modbus开源社区做出贡献的开发者。

---

## 🎉 开始学习

现在就开始你的C#后端学习之旅吧！

👉 **[Day 1: 环境搭建和Hello World](./Day1-环境搭建和HelloWorld.md)**

---

**祝你学习顺利！有任何问题欢迎交流！🚀**

*最后更新: 2025年10月15日*


