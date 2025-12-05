use sysinfo::{Disks, Networks, System};
use serde::Serialize;
use crate::wmi::{get_usb_devices as wmi_get_usb_devices, get_usb_controllers as wmi_get_usb_controllers};
pub use crate::wmi::{UsbDevice, UsbController};
use crate::wmi_impl::get_system_info;

#[derive(Serialize, Debug)]
pub struct HardwareData {
    pub system_name: String,
    pub kernel_version: String,
    pub os_version: String,
    pub host_name: String,
    pub cpu_count: usize,
    pub cpu_brand: String,
    pub total_memory: u64,
    pub disks: Vec<DiskInfo>,
    pub networks: Vec<NetworkInfo>,
    pub usb_devices: Vec<UsbDevice>,
    pub usb_controllers: Vec<UsbController>,
}

#[derive(Serialize, Debug)]
pub struct DiskInfo {
    pub name: String,
    pub file_system: String,
    pub total_space: u64,
    pub available_space: u64,
}

#[derive(Serialize, Debug)]
pub struct NetworkInfo {
    pub name: String,
    pub mac_address: String,
    pub ip_networks: Vec<String>,
}



pub fn get_hardware_info() -> HardwareData {
    // Try to get comprehensive system info via WMI first
    match get_system_info() {
        Ok(system_info) => {
            // Convert WMI system info to HardwareData
            let mut sys = System::new_all();
            sys.refresh_all();
            
            let disks_raw = Disks::new_with_refreshed_list();
            let disks: Vec<DiskInfo> = disks_raw.list().iter().map(|d| DiskInfo {
                name: d.name().to_string_lossy().to_string(),
                file_system: d.file_system().to_string_lossy().to_string(),
                total_space: d.total_space(),
                available_space: d.available_space(),
            }).collect();
            
            // Get network info from sysinfo (more complete than WMI)
            let networks_raw = Networks::new_with_refreshed_list();
            let networks: Vec<NetworkInfo> = networks_raw.iter().map(|(name, data)| NetworkInfo {
                name: name.to_string(),
                mac_address: data.mac_address().to_string(),
                ip_networks: data.ip_networks().iter().map(|ip| ip.to_string()).collect(),
            }).collect();
            
            let usb_devices = get_usb_devices().unwrap_or_default();
            let usb_controllers = get_usb_controllers().unwrap_or_default();
            
            HardwareData {
                system_name: system_info.computer_name.clone().unwrap_or_else(|| 
                    System::name().unwrap_or_default()),
                kernel_version: System::kernel_version().unwrap_or_default(),
                os_version: format!("{} {} {}", 
                    system_info.os_name.unwrap_or_default(),
                    system_info.os_version.unwrap_or_default(),
                    system_info.os_build.unwrap_or_default()),
                host_name: system_info.computer_name.unwrap_or_else(|| 
                    System::host_name().unwrap_or_default()),
                cpu_count: system_info.cpu_count.unwrap_or(0) as usize,
                cpu_brand: system_info.cpu_name.unwrap_or_else(|| 
                    sys.cpus().first().map(|c| c.brand().to_string()).unwrap_or_default()),
                total_memory: system_info.total_memory.unwrap_or(sys.total_memory()),
                disks,
                networks,
                usb_devices,
                usb_controllers,
            }
        },
        Err(e) => {
            eprintln!("Warning: Failed to get system info from WMI: {:?}. Falling back to sysinfo.", e);
            
            // Fallback to original implementation
            let mut sys = System::new_all();
            sys.refresh_all();
            
            let disks_raw = Disks::new_with_refreshed_list();
            let networks_raw = Networks::new_with_refreshed_list();
            
            let disks: Vec<DiskInfo> = disks_raw.list().iter().map(|d| DiskInfo {
                name: d.name().to_string_lossy().to_string(),
                file_system: d.file_system().to_string_lossy().to_string(),
                total_space: d.total_space(),
                available_space: d.available_space(),
            }).collect();
            
            let networks: Vec<NetworkInfo> = networks_raw.iter().map(|(name, data)| NetworkInfo {
                name: name.to_string(),
                mac_address: data.mac_address().to_string(),
                ip_networks: data.ip_networks().iter().map(|ip| ip.to_string()).collect(),
            }).collect();
            
            let usb_devices = get_usb_devices().unwrap_or_default();
            let usb_controllers = get_usb_controllers().unwrap_or_default();
            
            HardwareData {
                system_name: System::name().unwrap_or_default(),
                kernel_version: System::kernel_version().unwrap_or_default(),
                os_version: System::os_version().unwrap_or_default(),
                host_name: System::host_name().unwrap_or_default(),
                cpu_count: sys.cpus().len(),
                cpu_brand: sys.cpus().first().map(|c| c.brand().to_string()).unwrap_or_default(),
                total_memory: sys.total_memory(),
                disks,
                networks,
                usb_devices,
                usb_controllers,
            }
        }
    }
}

fn get_usb_devices() -> Result<Vec<UsbDevice>, Box<dyn std::error::Error>> {
    match wmi_get_usb_devices() {
        Ok(devices) => Ok(devices),
        Err(e) => {
            eprintln!("Warning: Failed to get USB devices from WMI: {:?}", e);
            // Fall back to empty list rather than crashing
            Ok(vec![])
        }
    }
}

fn get_usb_controllers() -> Result<Vec<UsbController>, Box<dyn std::error::Error>> {
    match wmi_get_usb_controllers() {
        Ok(controllers) => Ok(controllers),
        Err(e) => {
            eprintln!("Warning: Failed to get USB controllers from WMI: {:?}", e);
            // Fall back to empty list rather than crashing
            Ok(vec![])
        }
    }
}
