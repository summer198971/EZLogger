// 修复版本的日志解析器
class LogParser {
  constructor() {
    this.logs = [];
    this.battleLogs = [];
    this.entityLogs = [];
    this.errorLogs = [];
    this.currentMultiLineLog = null;
    this.categories = new Set();  // 用于存储所有模块名称
    this.networkLogs = [];  // 存储网络日志
    this.normalLogs = [];   // 存储普通日志
  }

  // 解析单行日志
  parseLine(line) {
    // 修复的正则表达式，支持实际的日志格式
    // 格式1: [!@#]16:23:18:576 [Log] [FileAppender] Log started
    // 格式2: [!@#]16:23:18.577[Log][F:0][Lifecycle] 应用暂停状态变更: False
    // 格式3: [!@#]16:23:21.706[Warning][F:1431][测试] 这是一条Warning级别的测试消息 #1
    
    const regex1 = /^\[!@#\](\d{2}:\d{2}:\d{2}[:\.]?\d{3})\s+\[(Log|Warning|Error|Assert|Exception)\]\s+\[([^\]]+)\]\s+(.*)/;
    const regex2 = /^\[!@#\](\d{2}:\d{2}:\d{2}[:\.]?\d{3})\[(Log|Warning|Error|Assert|Exception)\]\[F:(\d+)\]\[([^\]]+)\]\s+(.*)/;
    
    // 如果是新的日志头
    if (line.startsWith('[!@#]')) {
      // 先尝试格式2（带帧号）
      let match = line.match(regex2);
      let format = 2;
      
      if (!match) {
        // 再尝试格式1（不带帧号）
        match = line.match(regex1);
        format = 1;
      }
      
      if (match) {
        // 保存之前的多行日志（如果存在）
        const completedLog = this.currentMultiLineLog;
        // 创建新的日志条目
        this.currentMultiLineLog = this.createLogEntry(match, line, format);
        return completedLog;
      } else {
        console.warn('Failed to parse log header:', line);
        // 创建一个基本的日志条目，确保不丢失任何日志
        this.currentMultiLineLog = this.createBasicLogEntry(line);
        return null;
      }
    }
    
    // 处理多行内容（如Stack Trace）
    if (this.currentMultiLineLog) {
      // 追加内容
      this.currentMultiLineLog.message += '\n' + line;
      this.currentMultiLineLog.rawContent += '\n' + line;
    } else {
      // 如果没有正在处理的日志，创建新的日志条目
      this.currentMultiLineLog = this.createBasicLogEntry(line);
    }
    
    return null;
  }

  createLogEntry(match, originalLine, format) {
    let timestamp, type, frame, category, message;
    
    if (format === 2) {
      // [!@#]16:23:18.577[Log][F:0][Lifecycle] 应用暂停状态变更: False
      timestamp = match[1];
      type = match[2];
      frame = parseInt(match[3]) || 0;
      category = match[4];
      message = match[5];
    } else {
      // [!@#]16:23:18:576 [Log] [FileAppender] Log started
      timestamp = match[1];
      type = match[2];
      frame = 0;
      category = match[3];
      message = match[4];
    }

    // 标准化时间戳格式（统一使用点号分隔毫秒）
    timestamp = timestamp.replace(':', '.');

    // 检查是否为网络日志
    const isNetProto = category === 'NET_PROTO' || originalLine.includes('[NET_PROTO]');
    const isNetSend = category === 'NET_SEND' || originalLine.includes('[NET_SEND]');
    const isNet = category === 'NET' || originalLine.includes('[NET]');
    const networkType = isNetProto ? 'NET_PROTO' : (isNetSend ? 'NET_SEND' : (isNet ? 'NET' : null));
    const isNetworkLog = isNetProto || isNetSend || isNet;

    // 如果是网络日志，调整category和message
    let finalCategory = category;
    let finalMessage = message;
    let NetName = "";
    
    if (networkType) {
      finalCategory = networkType;
      finalMessage = `[${category}] ${message}`;
      NetName = category;
    }

    return {
      timestamp: timestamp,
      type: type,
      frame: frame,
      category: finalCategory,
      message: finalMessage,
      isClient: originalLine.includes('[CLIENT]'),
      clientColor: null,
      moduleColor: null,
      isNetProto,
      isNetSend,
      isNet,
      messageType: networkType ? category : null,
      NetName: NetName,
      isNetworkLog: isNetworkLog,
      rawContent: originalLine
    };
  }

  createBasicLogEntry(line) {
    // 尝试从行中提取时间戳
    const timeMatch = line.match(/\[!@#\](\d{2}:\d{2}:\d{2}[:\.]?\d{3})/);
    const timestamp = timeMatch ? timeMatch[1].replace(':', '.') : '--';
    
    return {
      timestamp: timestamp,
      type: 'Log',
      frame: 0,
      category: '--',
      message: line,
      isClient: false,
      clientColor: null,
      moduleColor: null,
      isNetProto: false,
      isNetSend: false,
      isNet: false,
      messageType: null,
      NetName: "",
      isNetworkLog: false,
      rawContent: line
    };
  }

  // 判断是否为网络日志
  isNetworkLog(log) {
    return log.isNetProto || log.isNetSend || log.isNet;
  }

  // 解析整个日志文件
  parseLogFile(content) {
    this.logs = [];  // 清空旧日志
    this.categories.clear();  // 清空旧的模块列表
    this.networkLogs = [];
    this.normalLogs = [];
    this.battleLogs = [];
    this.entityLogs = [];
    this.errorLogs = [];
    
    const lines = content.split('\n');
    this.currentMultiLineLog = null;
    
    lines.forEach(line => {
      const parsedLog = this.parseLine(line);
      if (parsedLog) {
        this.logs.push(parsedLog);
        this.categories.add(parsedLog.category);  // 收集模块名称
        
        // 分类网络和普通日志
        if (this.isNetworkLog(parsedLog)) {
          this.networkLogs.push(parsedLog);
        } else {
          this.normalLogs.push(parsedLog);
        }

        // 分类存储
        if (parsedLog.category.includes('fight')) {
          this.battleLogs.push(parsedLog);
        }
        if (parsedLog.message.includes('entity')) {
          this.entityLogs.push(parsedLog);
        }
        if (parsedLog.type === 'Error' || parsedLog.type === 'Exception') {
          this.errorLogs.push(parsedLog);
        }
      }
    });
    
    // 确保最后一条多行日志也被保存
    if (this.currentMultiLineLog) {
      this.logs.push(this.currentMultiLineLog);
      this.categories.add(this.currentMultiLineLog.category);
      if (this.isNetworkLog(this.currentMultiLineLog)) {
        this.networkLogs.push(this.currentMultiLineLog);
      } else {
        this.normalLogs.push(this.currentMultiLineLog);
      }
      this.currentMultiLineLog = null;
    }
    
    console.log(`解析完成: 共 ${this.logs.length} 条日志, ${this.categories.size} 个模块`);
    console.log('模块列表:', Array.from(this.categories));
  }

  // 获取战斗相关日志
  getBattleLogs() {
    return this.battleLogs;
  }

  // 获取实体相关日志
  getEntityLogs() {
    return this.entityLogs; 
  }

  // 获取错误日志
  getErrorLogs() {
    return this.errorLogs;
  }

  // 获取网络日志
  getNetworkLogs() {
    return this.networkLogs;
  }

  // 获取普通日志
  getNormalLogs() {
    return this.normalLogs;
  }

  // 按时间范围过滤日志
  filterByTimeRange(startTime, endTime) {
    return this.logs.filter(log => {
      return log.timestamp >= startTime && log.timestamp <= endTime;
    });
  }

  // 获取所有模块名称
  getCategories() {
    return Array.from(this.categories).sort();
  }
}
