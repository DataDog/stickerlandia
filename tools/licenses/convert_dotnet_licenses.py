#!/usr/bin/env python3
import json
import csv
import subprocess
import sys
import os
import requests
import time
from urllib.parse import quote

def normalize_license(license_name):
    """Normalize license names to SPDX identifiers - same as the Maven version"""
    if not license_name:
        return ''
    
    # Dictionary of license name variations to SPDX codes
    license_map = {
        'Apache License 2.0': 'Apache-2.0',
        'Apache License, Version 2.0': 'Apache-2.0',
        'The Apache Software License, Version 2.0': 'Apache-2.0',
        'The Apache License, Version 2.0': 'Apache-2.0',
        'Apache License Version 2.0': 'Apache-2.0',
        'Apache 2.0': 'Apache-2.0',
        'The Apache Software License': 'Apache-2.0',
        'Apache Software License - Version 2.0': 'Apache-2.0',
        'The Apache-2.0': 'Apache-2.0',
        'The MIT License': 'MIT',
        'MIT License': 'MIT',
        'BSD License 3': 'BSD-3-Clause',
        'BSD 2-Clause License': 'BSD-2-Clause',
        'The BSD 3-Clause License': 'BSD-3-Clause',
        'BSD-3-Clause': 'BSD-3-Clause',
        'BSD-2-Clause': 'BSD-2-Clause',
        'Eclipse Public License - v 1.0': 'EPL-1.0',
        'EPL-2.0 - Version 1.0': 'EPL-2.0',
        'EPL-2.0 v2.0': 'EPL-2.0',
        'Eclipse Public License - v 2.0': 'EPL-2.0',
        'Eclipse Public License v. 2.0': 'EPL-2.0',
        'Eclipse Public License, Version 2.0': 'EPL-2.0',
        'Eclipse Public License': 'EPL-2.0',
        'Eclipse Public License 2.0': 'EPL-2.0',
        'Eclipse Public License v2.0': 'EPL-2.0',
        'EPL 2.0': 'EPL-2.0',
        'EDL 1.0': 'EDL-1.0',
        'Eclipse Distribution License - v 1.0': 'EDL-1.0',
        'Eclipse Distribution License': 'EDL-1.0',
        'GPL2 w/ CPE': 'GPL-2.0-with-classpath-exception',
        'GNU General Public License, version 2 with the GNU Classpath Exception': 'GPL-2.0-with-classpath-exception',
        'CDDL-1.1 AND GPL-2.0-only WITH Classpath-exception-2.0': 'GPL-2.0-with-classpath-exception',
        'CDDL + GPLv2 with classpath exception': 'GPL-2.0-with-classpath-exception',
        'Universal Permissive License, Version 1.0': 'UPL-1.0',
        'GNU Library General Public License v2.1 or later': 'LGPL-2.1-or-later',
        'Public Domain': 'Public Domain',
        'MIT-0': 'MIT-0',
        'Apache-2.0': 'Apache-2.0',
        'EPL-2.0': 'EPL-2.0',
        'EPL-1.0': 'EPL-1.0',
        'MS-PL': 'MS-PL',
        'PostgreSQL': 'PostgreSQL'
    }
    
    return license_map.get(license_name, license_name)

def get_nuget_license_info(package_id, version):
    """Fetch license information from NuGet API"""
    # Known license mappings for packages that are difficult to detect
    known_licenses = {
        'coverlet.collector': 'MIT',
        'Datadog.Sma': 'Apache-2.0',
        'Microsoft.AspNetCore.Authentication.JwtBearer': 'MIT',
        'Amazon.Lambda.Core': 'Apache-2.0',
        'Amazon.Lambda.SNSEvents': 'Apache-2.0',
        'Amazon.Lambda.SQSEvents': 'Apache-2.0'
    }
    
    # Check known mappings first
    if package_id in known_licenses:
        return known_licenses[package_id]
    
    try:
        # Try NuGet API v3 first
        url = f"https://api.nuget.org/v3-flatcontainer/{package_id.lower()}/{version}/{package_id.lower()}.nuspec"
        response = requests.get(url, timeout=10)
        
        if response.status_code == 200:
            # Parse XML to extract license info
            import xml.etree.ElementTree as ET
            root = ET.fromstring(response.text)
            
            # Try multiple namespace variations
            namespaces = [
                {'ns': 'http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd'},
                {'ns': 'http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd'},
                {'ns': 'http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd'},
                {'ns': 'http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'},
                {}  # No namespace
            ]
            
            for ns in namespaces:
                # Try license expression first (newer format)
                if ns:
                    license_elem = root.find('.//ns:license', ns)
                else:
                    license_elem = root.find('.//license')
                
                if license_elem is not None and license_elem.get('type') == 'expression':
                    return license_elem.text
                
                # Try license URL
                if ns:
                    license_url = root.find('.//ns:licenseUrl', ns)
                else:
                    license_url = root.find('.//licenseUrl')
                
                if license_url is not None and license_url.text:
                    license_url_text = license_url.text.lower()
                    # Common license URL patterns
                    if 'mit' in license_url_text:
                        return 'MIT'
                    elif 'apache' in license_url_text and '2.0' in license_url_text:
                        return 'Apache-2.0'
                    elif 'bsd' in license_url_text:
                        return 'BSD-3-Clause'
                    elif 'mslicense' in license_url_text or 'microsoft' in license_url_text:
                        return 'MIT'  # Microsoft packages are typically MIT
                    else:
                        return license_url.text
                
                # If we found something, break out of namespace loop
                if license_elem is not None or license_url is not None:
                    break
        
        # Fallback: try the catalog API
        catalog_url = f"https://api.nuget.org/v3/registration5-semver1/{package_id.lower()}/{version}.json"
        response = requests.get(catalog_url, timeout=10)
        
        if response.status_code == 200:
            data = response.json()
            license_expr = data.get('licenseExpression')
            if license_expr:
                return license_expr
            
            license_url = data.get('licenseUrl')
            if license_url:
                license_url_text = license_url.lower()
                if 'mit' in license_url_text:
                    return 'MIT'
                elif 'apache' in license_url_text and '2.0' in license_url_text:
                    return 'Apache-2.0'
                elif 'microsoft' in license_url_text:
                    return 'MIT'
                return license_url
        
        # Additional fallback: try the v2 API
        v2_url = f"https://www.nuget.org/api/v2/Packages?$filter=Id%20eq%20%27{package_id}%27%20and%20Version%20eq%20%27{version}%27&$format=json"
        response = requests.get(v2_url, timeout=10)
        
        if response.status_code == 200:
            data = response.json()
            entries = data.get('d', {}).get('results', [])
            if entries:
                entry = entries[0]
                license_url = entry.get('LicenseUrl')
                if license_url:
                    license_url_text = license_url.lower()
                    if 'mit' in license_url_text:
                        return 'MIT'
                    elif 'apache' in license_url_text and '2.0' in license_url_text:
                        return 'Apache-2.0'
                    elif 'microsoft' in license_url_text:
                        return 'MIT'
                    return license_url
        
        return ''
        
    except Exception as e:
        print(f"Warning: Could not fetch license for {package_id}: {e}", file=sys.stderr)
        return ''

def get_dotnet_packages(solution_dir):
    """Get .NET package information using dotnet list package"""
    try:
        # Change to the solution directory
        result = subprocess.run(
            ['dotnet', 'list', 'package', '--format', 'json'],
            cwd=solution_dir,
            capture_output=True,
            text=True,
            check=True
        )
        return json.loads(result.stdout)
    except subprocess.CalledProcessError as e:
        print(f"Error running dotnet list package: {e}", file=sys.stderr)
        print(f"Stderr: {e.stderr}", file=sys.stderr)
        return None
    except json.JSONDecodeError as e:
        print(f"Error parsing JSON output: {e}", file=sys.stderr)
        return None

def convert_dotnet_to_csv_rows():
    """Convert .NET package data to CSV rows"""
    package_data = get_dotnet_packages('user-management')
    if not package_data:
        return []
    
    rows = []
    seen_packages = set()  # To deduplicate packages across projects
    
    for project in package_data.get('projects', []):
        project_path = project.get('path', '')
        
        for framework in project.get('frameworks', []):
            for package in framework.get('topLevelPackages', []):
                package_id = package.get('id', '')
                resolved_version = package.get('resolvedVersion', '')
                auto_referenced = package.get('autoReferenced', False)
                
                # Skip auto-referenced packages (these are typically framework packages)
                if auto_referenced:
                    continue
                
                # Create unique key to avoid duplicates
                package_key = f"{package_id}:{resolved_version}"
                if package_key in seen_packages:
                    continue
                seen_packages.add(package_key)
                
                # Fetch license information from NuGet API
                print(f"Fetching license for {package_id}:{resolved_version}...", file=sys.stderr)
                license_info = get_nuget_license_info(package_id, resolved_version)
                normalized_license = normalize_license(license_info)
                
                # Create CSV row
                component = 'user-management'
                origin = f'nuget:{package_id}:{resolved_version}'
                copyright = ''
                
                rows.append([component, origin, normalized_license, copyright])
                
                # Small delay to be respectful to NuGet API
                time.sleep(0.1)
    
    return rows

def append_to_csv(csv_file, new_rows):
    """Append rows to existing CSV file"""
    if not new_rows:
        print("No .NET packages to add")
        return
    
    # Check if file exists and read existing content
    file_exists = os.path.exists(csv_file)
    
    with open(csv_file, 'a', newline='') as f:
        writer = csv.writer(f)
        
        # If file doesn't exist, write header
        if not file_exists:
            writer.writerow(['Component', 'Origin', 'License', 'Copyright'])
        
        # Write new rows
        for row in new_rows:
            writer.writerow(row)

if __name__ == '__main__':
    csv_file = 'LICENSE-3rdparty.csv'
    dotnet_rows = convert_dotnet_to_csv_rows()
    append_to_csv(csv_file, dotnet_rows)
    print(f"Added {len(dotnet_rows)} .NET packages to {csv_file}")