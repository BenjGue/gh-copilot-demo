// =============================================================================
// Dokploy VM — one-shot Bicep template for the GitHub Copilot workshop
// =============================================================================
// Provisions a small Ubuntu 24.04 VM with:
//   - Public IP (Standard SKU)
//   - NSG opening TCP 22 / 80 / 443 / 3000
//   - cloud-init that installs Dokploy automatically on first boot
//
// After `az deployment group create` completes, browse to
//   http://<publicIp>:3000
// to finish the Dokploy admin setup.
//
// Deploy:
//   az group create -n rg-dokploy-workshop -l westeurope
//   az deployment group create `
//     -g rg-dokploy-workshop `
//     -f iac/bicep/dokploy-vm.bicep `
//     -p adminUsername=azureuser sshPublicKey="$(Get-Content ~/.ssh/id_rsa.pub -Raw)"
// =============================================================================

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('VM size. B2s (2 vCPU / 4 GB) fits a single-user pre-test; bump to D2s_v5/D4s_v5 for a shared workshop server. The D*as_v5 (AMD) family is included as a fallback when B-series capacity is restricted.')
@allowed([
  'Standard_B2s'
  'Standard_B2ms'
  'Standard_B2s_v2'
  'Standard_B2as_v2'
  'Standard_D2s_v5'
  'Standard_D2as_v5'
  'Standard_D2as_v7'
  'Standard_D4s_v5'
  'Standard_D4as_v5'
])
param vmSize string = 'Standard_B2s_v2'

@description('OS disk size in GB. Dokploy minimum is 30 GB.')
@minValue(30)
param osDiskSizeGB int = 30

@description('Linux admin username.')
param adminUsername string = 'azureuser'

@description('SSH public key (the full content of your id_rsa.pub).')
@secure()
param sshPublicKey string

@description('Short name used as prefix for all resources.')
@maxLength(12)
param namePrefix string = 'dokploy'

// -----------------------------------------------------------------------------
// Names
// -----------------------------------------------------------------------------
var suffix       = uniqueString(resourceGroup().id)
var vmName       = '${namePrefix}-vm'
var nicName      = '${namePrefix}-nic'
var vnetName     = '${namePrefix}-vnet'
var subnetName   = 'default'
var pipName      = '${namePrefix}-pip-${suffix}'
var nsgName      = '${namePrefix}-nsg'
var dnsLabel     = toLower('${namePrefix}-${suffix}')

// cloud-init: install Dokploy on first boot.
// Note: we install everything as root via `runcmd`; the Dokploy installer
// also bootstraps Docker if it is not already present.
var cloudInit = '''#cloud-config
package_update: true
package_upgrade: false
packages:
  - curl
  - ca-certificates
runcmd:
  - [ bash, -lc, "curl -sSL https://dokploy.com/install.sh | sh > /var/log/dokploy-install.log 2>&1" ]
'''

// -----------------------------------------------------------------------------
// Networking
// -----------------------------------------------------------------------------
resource nsg 'Microsoft.Network/networkSecurityGroups@2024-01-01' = {
  name: nsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'AllowSSH'
        properties: {
          priority: 1001
          access: 'Allow'
          direction: 'Inbound'
          protocol: 'Tcp'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '22'
        }
      }
      {
        name: 'AllowHTTP'
        properties: {
          priority: 1002
          access: 'Allow'
          direction: 'Inbound'
          protocol: 'Tcp'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '80'
        }
      }
      {
        name: 'AllowHTTPS'
        properties: {
          priority: 1003
          access: 'Allow'
          direction: 'Inbound'
          protocol: 'Tcp'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '443'
        }
      }
      {
        name: 'AllowDokployUI'
        properties: {
          priority: 1004
          access: 'Allow'
          direction: 'Inbound'
          protocol: 'Tcp'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '3000'
        }
      }
    ]
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-01-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [ '10.10.0.0/16' ]
    }
    subnets: [
      {
        name: subnetName
        properties: {
          addressPrefix: '10.10.1.0/24'
          networkSecurityGroup: {
            id: nsg.id
          }
        }
      }
    ]
  }
}

resource pip 'Microsoft.Network/publicIPAddresses@2024-01-01' = {
  name: pipName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    dnsSettings: {
      domainNameLabel: dnsLabel
    }
  }
}

resource nic 'Microsoft.Network/networkInterfaces@2024-01-01' = {
  name: nicName
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          subnet: {
            id: '${vnet.id}/subnets/${subnetName}'
          }
          publicIPAddress: {
            id: pip.id
          }
        }
      }
    ]
  }
}

// -----------------------------------------------------------------------------
// VM
// -----------------------------------------------------------------------------
resource vm 'Microsoft.Compute/virtualMachines@2024-03-01' = {
  name: vmName
  location: location
  properties: {
    hardwareProfile: {
      vmSize: vmSize
    }
    storageProfile: {
      imageReference: {
        publisher: 'Canonical'
        offer: 'ubuntu-24_04-lts'
        sku: 'server'
        version: 'latest'
      }
      osDisk: {
        name: '${vmName}-osdisk'
        createOption: 'FromImage'
        diskSizeGB: osDiskSizeGB
        managedDisk: {
          storageAccountType: 'Standard_LRS'
        }
      }
    }
    osProfile: {
      computerName: vmName
      adminUsername: adminUsername
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${adminUsername}/.ssh/authorized_keys'
              keyData: sshPublicKey
            }
          ]
        }
      }
      customData: base64(cloudInit)
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: nic.id
        }
      ]
    }
  }
}

// -----------------------------------------------------------------------------
// Outputs
// -----------------------------------------------------------------------------
output publicIp string = pip.properties.ipAddress
output fqdn string = pip.properties.dnsSettings.fqdn
output dokployUrl string = 'http://${pip.properties.dnsSettings.fqdn}:3000'
output sshCommand string = 'ssh ${adminUsername}@${pip.properties.dnsSettings.fqdn}'
