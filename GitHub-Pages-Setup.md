# GitHub Pages 部署指南

## 🎯 目标
为EZ Logger的Web日志查看器设置GitHub Pages在线访问。

## 📋 设置步骤

### 1. 在GitHub仓库中启用Pages

1. 进入你的GitHub仓库: `https://github.com/summer198971/EZLogger`
2. 点击 **Settings** 标签
3. 在左侧菜单中找到 **Pages**
4. 在 **Source** 设置中选择 **GitHub Actions**

### 2. 提交工作流文件

确保以下文件已经提交到仓库：
```
.github/workflows/deploy-web-viewer.yml  # 自动部署工作流
```

### 3. 触发第一次部署

推送代码到 `master` 或 `main` 分支：
```bash
git add .
git commit -m "添加GitHub Pages部署配置"
git push origin master
```

### 4. 查看部署状态

1. 在GitHub仓库页面点击 **Actions** 标签
2. 查看 "Deploy Web Log Viewer to GitHub Pages" 工作流
3. 等待部署完成（通常需要1-3分钟）

### 5. 访问在线版本

部署完成后，可以通过以下地址访问：

- **主页**: https://summer198971.github.io/EZLogger/
- **日志查看器**: https://summer198971.github.io/EZLogger/logread.html

## 🔄 自动更新机制

工作流配置为：
- 当 `Web/` 目录下的文件发生变化时自动重新部署
- 支持手动触发部署（在Actions页面）
- 只有主分支的变更会触发部署

## 🛠️ 故障排除

### 部署失败
1. 检查Actions页面的错误信息
2. 确认Pages设置中的Source为"GitHub Actions"
3. 确认仓库为public（或者有GitHub Pro账户）

### 访问404
1. 等待几分钟，GitHub Pages有时需要时间生效
2. 检查部署是否真正完成
3. 确认URL地址正确

### 权限问题
确保GitHub Actions有Pages部署权限：
1. Settings → Actions → General
2. Workflow permissions 设置为 "Read and write permissions"

## 📊 使用统计

GitHub Pages提供基本的访问统计：
1. Settings → Pages → 查看访问统计
2. 可以看到页面访问量和访问来源

## 🎯 自定义域名（可选）

如果你有自定义域名：
1. 在Pages设置中添加Custom domain
2. 配置DNS记录指向GitHub Pages
3. 启用HTTPS

## 📝 注意事项

- GitHub Pages对免费账户有使用限制（月100GB带宽）
- 文件大小限制为100MB
- 仓库大小建议不超过1GB
- 部署频率建议不超过每小时10次

---

设置完成后，记得更新README.md和文档中的在线访问链接！
