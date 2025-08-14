mergeInto(LibraryManager.library, {
    SyncFiles_Internal: function () {
        FS.syncfs(false, function (err) {
            if (err) console.error(err);
            else console.log("Files synced to IndexedDB!");
        });
    },

    // 单独下载每个日志文件的备用方案
    DownloadLogFiles_Individual: function (folderPath) {
        try {
            var entries = FS.readdir(folderPath);
            var fileCount = 0;
            
            for (var i = 0; i < entries.length; i++) {
                var entry = entries[i];
                if (entry === '.' || entry === '..') continue;
                
                var fullPath = folderPath + '/' + entry;
                try {
                    var stat = FS.stat(fullPath);
                    if (FS.isFile(stat.mode)) {
                        var fileData = FS.readFile(fullPath);
                        var blob = new Blob([fileData], {type: 'text/plain'});
                        var link = document.createElement('a');
                        link.href = URL.createObjectURL(blob);
                        link.download = entry;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                        URL.revokeObjectURL(link.href);
                        fileCount++;
                        
                        // 添加小延迟避免浏览器阻止多个下载
                        if (fileCount > 1) {
                            setTimeout(function(){}, 100);
                        }
                    }
                } catch (e) {
                    console.warn("Error downloading file: " + entry, e);
                }
            }
            
            if (fileCount === 0) {
                alert("No log files found to download.");
            } else {
                console.log("Downloaded " + fileCount + " log files individually");
            }
        } catch (e) {
            console.error("Error accessing log folder:", e);
            alert("Cannot access log folder: " + e.message);
        }
    },

    // 下载日志文件夹的所有内容为ZIP
    DownloadLogFolder_Internal: function (folderPathPtr) {
        var folderPath = UTF8ToString(folderPathPtr);
        
        // 本地备用下载函数
        function downloadIndividualFiles(path) {
            try {
                var entries = FS.readdir(path);
                var fileCount = 0;
                
                for (var i = 0; i < entries.length; i++) {
                    var entry = entries[i];
                    if (entry === '.' || entry === '..') continue;
                    
                    var fullPath = path + '/' + entry;
                    try {
                        var stat = FS.stat(fullPath);
                        if (FS.isFile(stat.mode)) {
                            var fileData = FS.readFile(fullPath);
                            var blob = new Blob([fileData], {type: 'text/plain'});
                            var link = document.createElement('a');
                            link.href = URL.createObjectURL(blob);
                            link.download = entry;
                            document.body.appendChild(link);
                            link.click();
                            document.body.removeChild(link);
                            URL.revokeObjectURL(link.href);
                            fileCount++;
                            
                            // 添加小延迟避免浏览器阻止多个下载
                            if (fileCount > 1) {
                                setTimeout(function(){}, 100);
                            }
                        }
                    } catch (e) {
                        console.warn("Error downloading file: " + entry, e);
                    }
                }
                
                if (fileCount === 0) {
                    alert("No log files found to download.");
                } else {
                    console.log("Downloaded " + fileCount + " log files individually");
                }
            } catch (e) {
                console.error("Error accessing log folder:", e);
                alert("Cannot access log folder: " + e.message);
            }
        }
        
        try {
            // 检查JSZip是否可用
            if (typeof JSZip === 'undefined') {
                console.warn("JSZip not found, downloading individual files instead");
                downloadIndividualFiles(folderPath);
                return;
            }

            var zip = new JSZip();
            var logFolder = zip.folder("EZLogger_Logs");
            var hasFiles = false;

            // 递归读取文件夹内容
            function addFolderToZip(currentPath, zipFolder) {
                try {
                    var entries = FS.readdir(currentPath);
                    for (var i = 0; i < entries.length; i++) {
                        var entry = entries[i];
                        if (entry === '.' || entry === '..') continue;
                        
                        var fullPath = currentPath + '/' + entry;
                        var stat = FS.stat(fullPath);
                        
                        if (FS.isDir(stat.mode)) {
                            var subFolder = zipFolder.folder(entry);
                            addFolderToZip(fullPath, subFolder);
                        } else if (FS.isFile(stat.mode)) {
                            var fileData = FS.readFile(fullPath);
                            zipFolder.file(entry, fileData);
                            hasFiles = true;
                        }
                    }
                } catch (e) {
                    console.warn("Error reading directory: " + currentPath, e);
                }
            }

            addFolderToZip(folderPath, logFolder);

            if (!hasFiles) {
                console.warn("No log files found in: " + folderPath);
                // 创建一个说明文件
                logFolder.file("README.txt", "No log files found. Logs will appear here when generated.");
                hasFiles = true;
            }

            if (hasFiles) {
                zip.generateAsync({type:"blob"}).then(function(content) {
                    var link = document.createElement('a');
                    link.href = URL.createObjectURL(content);
                    link.download = "EZLogger_Logs_" + new Date().toISOString().slice(0,19).replace(/:/g, '-') + ".zip";
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                    URL.revokeObjectURL(link.href);
                    console.log("Log folder downloaded as ZIP");
                });
            }
        } catch (e) {
            console.error("Error creating ZIP file:", e);
            // 调用备用方案
            downloadIndividualFiles(folderPath);
        }
    },

    // 在新标签页中显示日志文件列表
    ShowLogFilesList_Internal: function (folderPathPtr) {
        var folderPath = UTF8ToString(folderPathPtr);
        
        try {
            var entries = FS.readdir(folderPath);
            var htmlContent = `
                <!DOCTYPE html>
                <html>
                <head>
                    <title>EZLogger - Log Files</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }
                        .container { max-width: 800px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                        h1 { color: #333; border-bottom: 2px solid #007acc; padding-bottom: 10px; }
                        .file-list { list-style: none; padding: 0; }
                        .file-item { 
                            background: #f9f9f9; 
                            margin: 8px 0; 
                            padding: 12px; 
                            border-radius: 4px; 
                            border-left: 4px solid #007acc; 
                            display: flex; 
                            justify-content: space-between; 
                            align-items: center; 
                        }
                        .file-name { font-weight: bold; color: #333; }
                        .file-info { font-size: 0.9em; color: #666; }
                        .download-btn { 
                            background: #007acc; 
                            color: white; 
                            border: none; 
                            padding: 6px 12px; 
                            border-radius: 4px; 
                            cursor: pointer; 
                            text-decoration: none; 
                            display: inline-block; 
                        }
                        .download-btn:hover { background: #005999; }
                        .no-files { text-align: center; color: #666; padding: 40px; }
                        .download-all { 
                            background: #28a745; 
                            color: white; 
                            border: none; 
                            padding: 10px 20px; 
                            border-radius: 4px; 
                            cursor: pointer; 
                            margin-bottom: 20px; 
                            font-size: 1.1em;
                        }
                        .download-all:hover { background: #218838; }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <h1>🗂️ EZLogger - 日志文件</h1>
                        <p><strong>日志路径:</strong> <code>${folderPath}</code></p>
            `;

            var fileEntries = [];
            for (var i = 0; i < entries.length; i++) {
                var entry = entries[i];
                if (entry === '.' || entry === '..') continue;
                
                try {
                    var fullPath = folderPath + '/' + entry;
                    var stat = FS.stat(fullPath);
                    if (FS.isFile(stat.mode)) {
                        var fileData = FS.readFile(fullPath);
                        var sizeKB = Math.round(fileData.length / 1024 * 100) / 100;
                        var modTime = new Date(stat.mtime).toLocaleString();
                        
                        fileEntries.push({
                            name: entry,
                            size: sizeKB,
                            time: modTime,
                            data: fileData
                        });
                    }
                } catch (e) {
                    console.warn("Error reading file: " + entry, e);
                }
            }

            if (fileEntries.length > 0) {
                htmlContent += `
                    <button class="download-all" onclick="downloadAllFiles()">📦 下载所有日志文件</button>
                    <ul class="file-list">
                `;
                
                for (var j = 0; j < fileEntries.length; j++) {
                    var file = fileEntries[j];
                    htmlContent += `
                        <li class="file-item">
                            <div>
                                <div class="file-name">📄 ${file.name}</div>
                                <div class="file-info">大小: ${file.size} KB | 修改时间: ${file.time}</div>
                            </div>
                            <button class="download-btn" onclick="downloadFile('${file.name}', ${j})">下载</button>
                        </li>
                    `;
                }
                htmlContent += '</ul>';
            } else {
                htmlContent += '<div class="no-files">📭 暂无日志文件</div>';
            }

            htmlContent += `
                    </div>
                    <script>
                        var fileData = ${JSON.stringify(fileEntries.map(f => ({ name: f.name, data: Array.from(f.data) })))};
                        
                        function downloadFile(fileName, index) {
                            var data = new Uint8Array(fileData[index].data);
                            var blob = new Blob([data], {type: 'text/plain'});
                            var link = document.createElement('a');
                            link.href = URL.createObjectURL(blob);
                            link.download = fileName;
                            link.click();
                            URL.revokeObjectURL(link.href);
                        }
                        
                        function downloadAllFiles() {
                            for (var i = 0; i < fileData.length; i++) {
                                setTimeout(function(index) {
                                    return function() {
                                        downloadFile(fileData[index].name, index);
                                    };
                                }(i), i * 200); // 200ms间隔避免浏览器阻止
                            }
                        }
                    </script>
                </body>
                </html>
            `;

            var newWindow = window.open();
            newWindow.document.write(htmlContent);
            newWindow.document.close();
            
        } catch (e) {
            console.error("Error showing log files list:", e);
            alert("无法访问日志文件夹: " + e.message);
        }
    }
});