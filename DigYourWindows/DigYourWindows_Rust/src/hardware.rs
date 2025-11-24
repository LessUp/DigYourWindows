use serde::Serialize;
use sysinfo::{Disks, Networks, System};
use wmi::{COMLibrary, WMIConnection};

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
    pub usb_devices: Vec<UsbInfo>,
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

#[derive(Serialize, Debug, serde::Deserialize)]
pub struct UsbInfo {
    #[serde(rename = "DeviceID")]
    pub device_id: String,
    #[serde(rename = "Name")]
    pub name: Option<String>,
    #[serde(rename = "Description")]
    pub description: Option<String>,
    #[serde(rename = "Manufacturer")]
    pub manufacturer: Option<String>,
}

pub fn get_hardware_info() -> HardwareData {
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

fn get_usb_devices() -> Result<Vec<UsbInfo>, Box<dyn std::error::Error>> {
    let com_con = COMLibrary::new()?;
    let wmi_con = WMIConnection::new(com_con)?;

    // Query Win32_PnPEntity where DeviceID starts with "USB"
    // This is a simple filter.
    let results: Vec<UsbInfo> = wmi_con.raw_query("SELECT DeviceID, Name, Description, Manufacturer FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'")?;
    Ok(results)
}
