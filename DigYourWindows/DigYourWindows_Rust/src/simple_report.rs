use std::fs::File;
use std::io::Write;
use crate::hardware::HardwareData;
use crate::reliability::ReliabilityRecord;
use crate::events::LogEvent;

// Helper function to format WMI timestamp to readable format
fn format_wmi_timestamp(wmi_timestamp: &str) -> String {
    // WMI timestamp format: YYYYMMDDHHMMSS.MMMMMM+UUU
    // We need to parse this and convert to a readable format
    if wmi_timestamp.len() >= 14 {
        let year = &wmi_timestamp[0..4];
        let month = &wmi_timestamp[4..6];
        let day = &wmi_timestamp[6..8];
        let hour = &wmi_timestamp[8..10];
        let minute = &wmi_timestamp[10..12];
        let second = &wmi_timestamp[12..14];
        
        format!("{}-{}-{} {}:{}:{}", year, month, day, hour, minute, second)
    } else {
        wmi_timestamp.to_string()
    }
}

pub fn generate_simple_report(
    hardware: &HardwareData,
    reliability: &[ReliabilityRecord],
    events: &[LogEvent],
    output_path: &str
) -> Result<(), Box<dyn std::error::Error>> {
    let html_content = generate_html(hardware, reliability, events)?;
    
    let mut file = File::create(output_path)?;
    file.write_all(html_content.as_bytes())?;
    
    println!("Report generated: {}", output_path);
    Ok(())
}

fn generate_html(
    hardware: &HardwareData,
    reliability: &[ReliabilityRecord],
    events: &[LogEvent]
) -> Result<String, Box<dyn std::error::Error>> {
    let mut html = String::new();
    
    // HTML Header
    html.push_str("<!DOCTYPE html>\n");
    html.push_str("<html lang=\"zh-CN\">\n");
    html.push_str("<head>\n");
    html.push_str("    <meta charset=\"UTF-8\">\n");
    html.push_str("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n");
    html.push_str("    <title>DigYourWindows 深度诊断报告</title>\n");
    html.push_str("    <link href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css\" rel=\"stylesheet\">\n");
    html.push_str("    <script src=\"https://code.jquery.com/jquery-3.7.0.min.js\"></script>\n");
    html.push_str("    <script src=\"https://cdn.datatables.net/1.13.4/js/jquery.dataTables.min.js\"></script>\n");
    html.push_str("    <link href=\"https://cdn.datatables.net/1.13.4/css/dataTables.bootstrap5.min.css\" rel=\"stylesheet\">\n");
    html.push_str("</head>\n");
    html.push_str("<body>\n");
    html.push_str("    <div class=\"container py-4\">\n");
    html.push_str("        <header class=\"pb-3 mb-4 border-bottom\">\n");
    html.push_str("            <h1 class=\"display-5 fw-bold text-primary\">DigYourWindows 诊断报告</h1>\n");
    html.push_str(&format!("            <p class=\"col-md-8 fs-4 text-muted\">生成时间: {}</p>\n", 
        chrono::Local::now().format("%Y-%m-%d %H:%M:%S").to_string()));
    html.push_str("        </header>\n");

    // System Info
    html.push_str("        <section>\n");
    html.push_str("            <h2 class=\"section-title\">系统概览</h2>\n");
    html.push_str("            <div class=\"row\">\n");
    
    html.push_str("                <div class=\"col-md-3\">\n");
    html.push_str("                    <div class=\"card text-center p-3\">\n");
    html.push_str("                        <h5>主机名</h5>\n");
    html.push_str(&format!("                        <p class=\"lead\">{}</p>\n", hardware.host_name));
    html.push_str("                    </div>\n");
    html.push_str("                </div>\n");
    
    html.push_str("                <div class=\"col-md-3\">\n");
    html.push_str("                    <div class=\"card text-center p-3\">\n");
    html.push_str("                        <h5>操作系统</h5>\n");
    html.push_str(&format!("                        <p class=\"lead\">{}</p>\n", hardware.os_version));
    html.push_str("                    </div>\n");
    html.push_str("                </div>\n");
    
    html.push_str("                <div class=\"col-md-3\">\n");
    html.push_str("                    <div class=\"card text-center p-3\">\n");
    html.push_str("                        <h5>CPU</h5>\n");
    html.push_str(&format!("                        <p class=\"small\">{} ({} Cores)</p>\n", hardware.cpu_brand, hardware.cpu_count));
    html.push_str("                    </div>\n");
    html.push_str("                </div>\n");
    
    html.push_str("                <div class=\"col-md-3\">\n");
    html.push_str("                    <div class=\"card text-center p-3\">\n");
    html.push_str("                        <h5>内存</h5>\n");
    html.push_str(&format!("                        <p class=\"lead\">{:.2} GB</p>\n", hardware.total_memory as f64 / 1024.0 / 1024.0 / 1024.0));
    html.push_str("                    </div>\n");
    html.push_str("                </div>\n");
    
    html.push_str("            </div>\n");
    html.push_str("        </section>\n");

    // Disk Information
    html.push_str("        <section>\n");
    html.push_str("            <h2 class=\"section-title\">磁盘信息</h2>\n");
    html.push_str("            <div class=\"row mb-3\">\n");
    
    for disk in &hardware.disks {
        let usage_percent = ((disk.total_space - disk.available_space) as f64 / disk.total_space as f64) * 100.0;
        html.push_str("                <div class=\"col-md-4\">\n");
        html.push_str("                    <div class=\"card p-3\">\n");
        html.push_str(&format!("                        <h6>{} ({})</h6>\n", disk.name, disk.file_system));
        html.push_str(&format!("                        <div class=\"progress mb-2\">\n"));
        html.push_str(&format!("                            <div class=\"progress-bar\" role=\"progressbar\" style=\"width: {:.0}%\"></div>\n", usage_percent));
        html.push_str("                        </div>\n");
        html.push_str(&format!("                        <small>可用: {:.2} GB / 总计: {:.2} GB</small>\n", 
            disk.available_space as f64 / 1024.0 / 1024.0 / 1024.0,
            disk.total_space as f64 / 1024.0 / 1024.0 / 1024.0));
        html.push_str("                    </div>\n");
        html.push_str("                </div>\n");
    }
    
    html.push_str("            </div>\n");
    html.push_str("        </section>\n");

    // Reliability Records
    html.push_str("        <section>\n");
    html.push_str(&format!("            <h2 class=\"section-title\">系统可靠性记录 ({})</h2>\n", reliability.len()));
    html.push_str("            <div class=\"card p-3\">\n");
    html.push_str("                <table id=\"reliabilityTable\" class=\"table table-striped table-hover\" style=\"width:100%\">\n");
    html.push_str("                    <thead>\n");
    html.push_str("                        <tr>\n");
    html.push_str("                            <th>时间</th>\n");
    html.push_str("                            <th>产品名称</th>\n");
    html.push_str("                            <th>类型</th>\n");
    html.push_str("                            <th>消息</th>\n");
    html.push_str("                        </tr>\n");
    html.push_str("                    </thead>\n");
    html.push_str("                    <tbody>\n");
    
    for record in reliability {
        html.push_str("                        <tr>\n");
        html.push_str(&format!("                            <td style=\"white-space:nowrap;\">{}</td>\n", 
            format_wmi_timestamp(&record.time_generated)));
        html.push_str(&format!("                            <td>{}</td>\n", 
            record.product_name.as_ref().unwrap_or(&"Unknown".to_string())));
        
        let record_type_str = match record.record_type {
            Some(1) => "应用崩溃",
            Some(2) => "系统崩溃",
            Some(3) => "其他事件",
            _ => "未知类型"
        };
        
        html.push_str(&format!("                            <td><span class=\"badge bg-warning\">{}</span></td>\n", record_type_str));
        
        let message = record.message.as_ref()
            .map(|m| if m.len() > 200 { format!("{}...", &m[..200]) } else { m.clone() })
            .unwrap_or_default();
        html.push_str(&format!("                            <td class=\"text-break\">{}</td>\n", message));
        html.push_str("                        </tr>\n");
    }
    
    html.push_str("                    </tbody>\n");
    html.push_str("                </table>\n");
    html.push_str("            </div>\n");
    html.push_str("        </section>\n");

    // Events
    html.push_str("        <section>\n");
    html.push_str(&format!("            <h2 class=\"section-title\">最近错误日志 ({})</h2>\n", events.len()));
    html.push_str("            <div class=\"card p-3\">\n");
    html.push_str("                <table id=\"eventsTable\" class=\"table table-striped table-hover\" style=\"width:100%\">\n");
    html.push_str("                    <thead>\n");
    html.push_str("                        <tr>\n");
    html.push_str("                            <th>时间</th>\n");
    html.push_str("                            <th>来源</th>\n");
    html.push_str("                            <th>类型</th>\n");
    html.push_str("                            <th>消息</th>\n");
    html.push_str("                        </tr>\n");
    html.push_str("                    </thead>\n");
    html.push_str("                    <tbody>\n");
    
    for event in events {
        html.push_str("                        <tr>\n");
        html.push_str(&format!("                            <td style=\"white-space:nowrap;\">{}</td>\n", event.time_generated));
        html.push_str(&format!("                            <td>{}</td>\n", 
            event.source_name.as_ref().unwrap_or(&"Unknown".to_string())));
        html.push_str(&format!("                            <td><span class=\"badge bg-danger\">{}</span></td>\n", 
            event.event_type.as_ref().unwrap_or(&"Error".to_string())));
        
        let message = event.message.as_ref()
            .map(|m| if m.len() > 200 { format!("{}...", &m[..200]) } else { m.clone() })
            .unwrap_or_default();
        html.push_str(&format!("                            <td class=\"text-break\">{}</td>\n", message));
        html.push_str("                        </tr>\n");
    }
    
    html.push_str("                    </tbody>\n");
    html.push_str("                </table>\n");
    html.push_str("            </div>\n");
    html.push_str("        </section>\n");

    // Close HTML
    html.push_str("    </div>\n");
    html.push_str("    <script src=\"https://cdn.datatables.net/1.13.4/js/dataTables.bootstrap5.min.js\"></script>\n");
    html.push_str("    <script>\n");
    html.push_str("        $(document).ready(function () {\n");
    html.push_str("            $('#reliabilityTable').DataTable({\n");
    html.push_str("                \"order\": [[ 0, \"desc\" ]]\n");
    html.push_str("            });\n");
    html.push_str("            $('#eventsTable').DataTable({\n");
    html.push_str("                \"order\": [[ 0, \"desc\" ]]\n");
    html.push_str("            });\n");
    html.push_str("        });\n");
    html.push_str("    </script>\n");
    html.push_str("</body>\n");
    html.push_str("</html>\n");
    
    Ok(html)
}