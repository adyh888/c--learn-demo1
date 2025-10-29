# Day 8: MQTT协议入门与原理

> **学习目标**: 理解MQTT协议、发布/订阅模式、物联网应用场景
>
> **预计时间**: 2-3小时

---

## 📚 什么是MQTT？

**MQTT** = Message Queuing Telemetry Transport（消息队列遥测传输协议）

### 特点

- 轻量级（适合资源受限设备）
- 发布/订阅模式（不是请求/响应）
- 低带宽、低功耗
- 支持离线消息
- 三种QoS级别

### 应用场景

- 🏭 工业物联网（传感器数据采集）
- 🏠 智能家居（设备控制）
- 🚗 车联网（车辆状态上报）
- 📱 移动推送（APP消息推送）

---

## 🔄 MQTT vs HTTP

**🔵 前端类比理解:**

| 特性   | HTTP/REST API | MQTT  | WebSocket |
|------|---------------|-------|-----------|
| 通信模式 | 请求/响应         | 发布/订阅 | 双向通信      |
| 连接   | 短连接           | 长连接   | 长连接       |
| 主动推送 | ❌ (需轮询)       | ✅     | ✅         |
| 带宽消耗 | 较高            | 低     | 中等        |
| 适用场景 | Web API       | IoT设备 | 实时应用      |

**举例说明:**

```javascript
// HTTP方式：前端需要不断轮询
setInterval(async () => {
  const data = await fetch('/api/temperature');
  console.log(data);
}, 5000);  // 每5秒请求一次

// MQTT方式：订阅一次，自动接收
mqtt.subscribe('device/temperature');
mqtt.on('message', (topic, message) => {
  console.log('收到新数据:', message);  // 数据到达自动触发
});
```

---

## 🏗️ MQTT架构

```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│  Publisher   │         │    Broker    │         │  Subscriber  │
│  (发布者)     │ ──pub─→ │   (代理)      │ ─sub─→ │   (订阅者)    │
│              │         │              │         │              │
│  温度传感器   │         │  MQTT服务器   │         │   监控系统    │
└──────────────┘         └──────────────┘         └──────────────┘
```

**核心概念:**

1. **Broker（代理/服务器）**
    - 类似消息中转站
    - 接收所有发布的消息
    - 转发给对应的订阅者
    - 常用：Mosquitto, EMQX, HiveMQ

2. **Publisher（发布者）**
    - 发送消息到指定主题
    - 例如：温度传感器发布温度数据

3. **Subscriber（订阅者）**
    - 订阅感兴趣的主题
    - 自动接收该主题的消息
    - 例如：监控系统订阅所有传感器数据

---

## 📝 MQTT主题（Topic）

**主题格式:** 使用 `/` 分隔的层级结构

```
factory/workshop1/temperature     ← 工厂/车间1/温度
factory/workshop1/humidity        ← 工厂/车间1/湿度
factory/workshop2/temperature     ← 工厂/车间2/温度
home/bedroom/light/status         ← 家/卧室/灯/状态
```

**通配符:**

```
+  单层通配符
#  多层通配符

示例：
factory/+/temperature             ← 所有车间的温度
factory/workshop1/#               ← 车间1的所有数据
factory/#                         ← 工厂的所有数据
```

**🔵 前端类比:**

```javascript
// 类似前端的事件系统
eventBus.on('user:login', handler);        ← MQTT订阅
eventBus.emit('user:login', data);         ← MQTT发布

// 类似URL路由
/api/factory/workshop1/temperature         ← HTTP
factory/workshop1/temperature              ← MQTT
```

---

## 🎯 QoS（服务质量等级）

| QoS | 名称            | 说明         | 使用场景   |
|-----|---------------|------------|--------|
| 0   | At most once  | 最多一次（可能丢失） | 不重要的数据 |
| 1   | At least once | 至少一次（可能重复） | 一般数据   |
| 2   | Exactly once  | 恰好一次（不丢不重） | 重要指令   |

**🔵 前端类比:**

```javascript
// QoS 0 类似于：
fire-and-forget();  // 发送了就不管了

// QoS 1 类似于：
await fetch('/api/data');  // 等待确认，但可能重复

// QoS 2 类似于：
await db.transaction();  // 事务保证，恰好一次
```

---

## 🛠️ MQTT消息格式

### 消息结构

```json
{
  "topic": "factory/workshop1/temperature",
  "payload": {
    "deviceId": "sensor-001",
    "value": 25.5,
    "unit": "°C",
    "timestamp": "2025-10-15T10:30:00Z"
  },
  "qos": 1,
  "retain": false
}
```

### Retain消息

```
retain: false  ← 普通消息（不保留）
retain: true   ← 保留消息（新订阅者也能收到最后一条）
```

**使用场景:**

```
设备状态：retain=true   ← 新订阅者需要知道当前状态
历史数据：retain=false  ← 只关心实时数据
```

---

## 🔧 安装MQTT Broker（服务器）

### 方式1：使用Docker（推荐）

```bash
# 安装Mosquitto（最流行的MQTT Broker）
docker run -d \
  --name mosquitto \
  -p 1883:1883 \
  -p 9001:9001 \
  eclipse-mosquitto

# 测试是否运行
docker ps
```

### 方式2：使用在线Broker（学习用）

免费公共Broker:

- `broker.emqx.io:1883`
- `test.mosquitto.org:1883`

**⚠️ 注意：公共Broker仅用于学习，不要发送敏感数据！**

---

## 🎮 MQTT客户端工具测试

### 使用MQTTX（GUI工具）

1. 下载：https://mqttx.app/
2. 安装后创建连接：
   ```
   Host: broker.emqx.io
   Port: 1883
   ```

3. **订阅主题:**
   ```
   主题: test/my-demo
   QoS: 1
   ```

4. **发布消息:**
   ```
   主题: test/my-demo
   消息: {"temperature": 25.5}
   QoS: 1
   ```

5. 观察订阅窗口收到消息

### 使用命令行工具

```bash
# 安装mosquitto客户端
brew install mosquitto  # macOS

# 订阅主题
mosquitto_sub -h broker.emqx.io -t "test/my-demo" -v

# 发布消息
mosquitto_pub -h broker.emqx.io -t "test/my-demo" -m "Hello MQTT"
```

---

## 💡 MQTT应用场景示例

### 场景1：温度传感器监控

```
┌──────────────┐
│ 温度传感器    │
│  (发布者)     │
└──────────────┘
       │
       │ 每10秒发布一次
       │ Topic: factory/workshop1/temperature
       │ Payload: {"deviceId":"sensor-001","value":25.5}
       ↓
┌──────────────┐
│  MQTT Broker │
└──────────────┘
       │
       ├─→ ┌──────────────┐
       │   │  监控面板    │ ← 订阅显示
       │   └──────────────┘
       │
       ├─→ ┌──────────────┐
       │   │  数据库服务   │ ← 订阅存储
       │   └──────────────┘
       │
       └─→ ┌──────────────┐
           │  报警系统    │ ← 订阅检测异常
           └──────────────┘
```

### 场景2：智能灯控制

```
┌──────────────┐
│   手机APP    │
│  (发布者)     │
└──────────────┘
       │
       │ 发布指令
       │ Topic: home/bedroom/light/command
       │ Payload: {"action":"on","brightness":80}
       ↓
┌──────────────┐
│  MQTT Broker │
└──────────────┘
       │
       ↓
┌──────────────┐
│   智能灯     │
│  (订阅者)     │
└──────────────┘
       │
       │ 状态反馈
       │ Topic: home/bedroom/light/status
       │ Payload: {"status":"on","brightness":80}
       ↓
┌──────────────┐
│  MQTT Broker │
└──────────────┘
       │
       ↓
┌──────────────┐
│   手机APP    │
│  (订阅者)     │ ← 更新UI
└──────────────┘
```

---

## 📝 MQTT消息设计最佳实践

### 1. 主题命名规范

```
✅ 好的命名：
company/site/area/deviceType/deviceId/metric
example: acme/factory1/workshop/sensor/temp001/temperature

❌ 不好的命名：
temp           ← 太简单
my-topic-123   ← 没有层级结构
```

### 2. 消息负载格式

```json
// ✅ 推荐：JSON格式
{
  "deviceId": "sensor-001",
  "timestamp": "2025-10-15T10:30:00Z",
  "data": {
    "temperature": 25.5,
    "humidity": 60.2
  },
  "unit": {
    "temperature": "°C",
    "humidity": "%"
  }
}

// ✅ 备选：简洁格式（资源受限设备）
{
  "d": "sensor-001",
  "t": 1697364600,
  "v": 25.5
}
```

### 3. 主题设计示例

```
工厂IoT系统：
├─ factory/{factoryId}/device/{deviceId}/data        ← 设备数据
├─ factory/{factoryId}/device/{deviceId}/status      ← 设备状态
├─ factory/{factoryId}/device/{deviceId}/command     ← 设备指令
├─ factory/{factoryId}/alarm/{level}                 ← 报警信息
└─ factory/{factoryId}/statistics/realtime           ← 实时统计
```

---

## 📊 MQTT vs 其他协议对比

| 协议            | 传输模式  | 带宽消耗 | 实时性 | 复杂度 | 使用场景    |
|---------------|-------|------|-----|-----|---------|
| **MQTT**      | 发布/订阅 | 低    | 高   | 简单  | IoT设备   |
| **HTTP**      | 请求/响应 | 高    | 低   | 简单  | Web API |
| **WebSocket** | 双向通信  | 中    | 高   | 中等  | 实时应用    |
| **CoAP**      | 请求/响应 | 极低   | 中   | 中等  | 资源受限设备  |
| **Modbus**    | 主从    | 低    | 中   | 简单  | 工业设备    |

---

## 📝 今日总结

### ✅ 你学会了：

- [x] MQTT协议的概念和特点
- [x] 发布/订阅模式
- [x] MQTT主题和通配符
- [x] QoS服务质量等级
- [x] MQTT Broker的作用
- [x] MQTT应用场景
- [x] 使用MQTTX工具测试

### 🔑 核心概念：

| 概念        | 说明    | 前端类比         |
|-----------|-------|--------------|
| Broker    | 消息中转站 | WebSocket服务器 |
| Topic     | 消息主题  | 事件名称         |
| Publish   | 发布消息  | emit事件       |
| Subscribe | 订阅消息  | on监听         |
| QoS       | 服务质量  | 消息可靠性        |

---

## 🎯 明日预告：Day 9 - C#实现MQTT客户端

明天你将学习：

- 使用MQTTnet库
- 实现MQTT客户端
- 发布和订阅消息
- 与ASP.NET Core集成

---

## 💾 作业

1. 安装并测试MQTT Broker（Mosquitto或使用公共Broker）
2. 使用MQTTX工具订阅和发布消息
3. 设计你的IoT项目的主题结构
4. 思考：
    - 什么场景适合MQTT？什么场景适合HTTP？
    - 如何保证MQTT消息的安全性？

---

**MQTT原理理解了！明天开始写代码！🚀**


