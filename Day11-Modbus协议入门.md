# Day 11: Modbus协议入门

> **学习目标**: 理解Modbus协议、工业设备通信、寄存器读写
> 
> **预计时间**: 2-3小时

---

## 📚 什么是Modbus？

**Modbus** 是工业自动化中最常用的通信协议，用于PLC、传感器、仪表等设备通信。

### 特点
- 主从架构（Master-Slave）
- 简单可靠
- 广泛支持
- 开放标准

### 应用场景
- 🏭 工厂自动化（PLC控制）
- 🔌 电力监控（智能电表）
- 🌡️ 楼宇自动化（HVAC系统）
- 💧 水处理（泵站控制）

---

## 🔄 Modbus vs MQTT

| 特性 | Modbus | MQTT |
|-----|--------|------|
| 架构 | 主从（Master-Slave） | 发布/订阅 |
| 协议层 | 应用层 | 应用层 |
| 传输方式 | TCP/RTU/ASCII | TCP |
| 实时性 | 高（轮询） | 高（推送） |
| 场景 | 工业设备 | IoT设备 |
| 带宽消耗 | 低 | 低 |

**🔵 前端类比:**
```
Modbus ≈ RPC调用（客户端主动请求服务器）
MQTT ≈ WebSocket（服务器可以主动推送）
```

---

## 🏗️ Modbus架构

```
┌───────────────┐         ┌───────────────┐
│    Master     │         │     Slave     │
│   (主站)       │  ──req→ │    (从站)      │
│               │  ←─res─ │               │
│  Modbus客户端  │         │  PLC/传感器    │
└───────────────┘         └───────────────┘

Master主动发起请求，Slave被动响应
```

### Modbus三种模式

1. **Modbus RTU** - 串口通信（RS232/RS485）
   - 二进制格式
   - 高效，常用于现场设备

2. **Modbus TCP** - 以太网通信
   - 基于TCP/IP
   - 易于集成，适合网络环境

3. **Modbus ASCII** - 串口通信
   - ASCII字符格式
   - 易于调试，但效率低

**今天我们重点学习 Modbus TCP**

---

## 📝 Modbus数据模型

Modbus定义了4种数据区域：

| 数据类型 | 地址范围 | 读写 | 说明 |
|---------|---------|------|------|
| **Coils（线圈）** | 00001-09999 | 读/写 | 开关量输出（DO） |
| **Discrete Inputs（离散输入）** | 10001-19999 | 只读 | 开关量输入（DI） |
| **Input Registers（输入寄存器）** | 30001-39999 | 只读 | 模拟量输入（AI） |
| **Holding Registers（保持寄存器）** | 40001-49999 | 读/写 | 模拟量输出（AO） |

**🔵 前端类比:**
```javascript
// Modbus 类似于操作设备的"状态"和"属性"
device.coils[0] = true;           // 写线圈（开关灯）
const temp = device.registers[0]; // 读寄存器（读温度）
```

### 寄存器示例

```
设备地址: 192.168.1.100:502
从站ID: 1

寄存器映射:
├─ 保持寄存器 (40001-40010)
│  ├─ 40001: 温度设定值 (25°C)
│  ├─ 40002: 湿度设定值 (60%)
│  └─ 40003: 运行模式 (0=停止, 1=运行)
│
└─ 输入寄存器 (30001-30010)
   ├─ 30001: 当前温度 (24.5°C)
   ├─ 30002: 当前湿度 (58%)
   └─ 30003: 运行状态 (1=运行中)
```

---

## 🛠️ Modbus功能码

常用功能码：

| 功能码 | 名称 | 说明 |
|-------|------|------|
| **01** | Read Coils | 读线圈状态 |
| **02** | Read Discrete Inputs | 读离散输入 |
| **03** | Read Holding Registers | 读保持寄存器 |
| **04** | Read Input Registers | 读输入寄存器 |
| **05** | Write Single Coil | 写单个线圈 |
| **06** | Write Single Register | 写单个寄存器 |
| **15** | Write Multiple Coils | 写多个线圈 |
| **16** | Write Multiple Registers | 写多个寄存器 |

**示例：读取温度**
```
请求：
功能码: 03 (读保持寄存器)
起始地址: 0000 (寄存器40001)
数量: 0001 (读1个寄存器)

响应：
功能码: 03
字节数: 02
数据: 00 FF (255 = 25.5°C，需除以10)
```

---

## 🔧 Modbus数据解析

### 整数类型

```csharp
// 16位整数（1个寄存器）
ushort value = registers[0];  // 0-65535

// 32位整数（2个寄存器）
int value = (registers[0] << 16) | registers[1];
```

### 浮点数

```csharp
// 32位浮点数（2个寄存器）
byte[] bytes = new byte[4];
Buffer.BlockCopy(registers, 0, bytes, 0, 4);
float value = BitConverter.ToSingle(bytes, 0);
```

### 带符号数

```csharp
// 有符号16位整数
short value = (short)registers[0];  // -32768 到 32767
```

### 缩放值

```csharp
// 温度值（寄存器值 / 10）
float temperature = registers[0] / 10.0f;  // 255 → 25.5°C

// 压力值（寄存器值 / 100）
float pressure = registers[0] / 100.0f;  // 1234 → 12.34 bar
```

---

## 🎯 Modbus通信流程

```
┌─────────────┐
│  C# 程序     │
│  (Master)   │
└─────────────┘
       │
       │ 1. 连接到设备
       │ TCP Connect("192.168.1.100:502")
       ↓
┌─────────────┐
│  PLC设备    │
│  (Slave)    │
└─────────────┘
       │
       │ 2. 发送读取请求
       │ ReadHoldingRegisters(startAddr=0, count=10)
       ↓
┌─────────────┐
│   响应数据   │
│  [255, 600] │
└─────────────┘
       │
       │ 3. 解析数据
       │ temp = 255/10 = 25.5°C
       │ humidity = 600/10 = 60%
       ↓
┌─────────────┐
│  存储/展示   │
└─────────────┘
```

---

## 📊 Modbus与MQTT结合

**典型IoT架构：**

```
┌─────────────┐
│  PLC/传感器  │ ← Modbus RTU/TCP
│  (Modbus)   │
└─────────────┘
       ↓
┌─────────────┐
│  网关/边缘   │ ← Modbus → MQTT 转换
│  (C#程序)   │
└─────────────┘
       ↓ MQTT
┌─────────────┐
│ MQTT Broker │
└─────────────┘
       ↓
┌─────────────┐
│  云平台/Web  │
│  (监控系统)  │
└─────────────┘
```

**数据流转：**
1. C#程序通过Modbus定时读取PLC数据
2. 解析后转换为JSON格式
3. 通过MQTT发布到云端
4. 监控系统订阅显示

---

## 🛠️ Modbus模拟器（用于测试）

### 推荐工具

1. **ModSim** (Windows)
   - 免费的Modbus从站模拟器
   - 可模拟寄存器和线圈

2. **Modbus Doctor** (跨平台)
   - 图形化Modbus测试工具

3. **pyModSlave** (Python)
   ```bash
   pip install pyModSlave
   pyModSlave --port 502
   ```

### Docker方式（推荐）

```bash
# 运行Modbus TCP模拟器
docker run -d \
  --name modbus-simulator \
  -p 502:502 \
  oitc/modbus-server
```

---

## 📝 Modbus通信示例

### 场景：读取温度传感器

```
设备信息:
- IP: 192.168.1.100
- 端口: 502
- 从站ID: 1
- 寄存器地址: 30001 (实际地址 0)
- 数据格式: 16位整数，除以10得到实际温度

通信步骤:
1. 建立TCP连接
2. 发送读取请求 (功能码03, 地址0, 数量1)
3. 接收响应 (2字节数据)
4. 解析: 255 / 10 = 25.5°C
```

---

## 📝 今日总结

### ✅ 你学会了：
- [x] Modbus协议的概念和特点
- [x] Modbus三种模式（RTU/TCP/ASCII）
- [x] 四种数据区域（线圈、离散输入、输入寄存器、保持寄存器）
- [x] 常用功能码
- [x] 数据解析方法
- [x] Modbus在IoT中的应用

### 🔑 核心概念对比：

| 概念 | Modbus | 前端类比 |
|-----|--------|---------|
| Master | 主站 | HTTP客户端 |
| Slave | 从站 | HTTP服务器 |
| 寄存器 | 数据存储 | 对象属性 |
| 功能码 | 操作类型 | HTTP方法(GET/POST) |
| 地址 | 数据位置 | URL路径 |

---

## 🎯 明日预告：Day 12 - C#实现Modbus客户端

明天你将学习：
- 使用NModbus库
- 实现Modbus TCP客户端
- 读写寄存器
- 数据解析和转换

---

## 💾 作业

1. 安装Modbus模拟器并测试连接
2. 理解4种数据区域的区别
3. 手动计算Modbus数据解析
4. 思考：
   - Modbus和HTTP的相似之处？
   - 如何将Modbus数据发布到MQTT？

---

**Modbus原理理解了！明天开始实现代码！🚀**


