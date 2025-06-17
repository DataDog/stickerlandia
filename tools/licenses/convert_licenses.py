#!/usr/bin/env python3
import xml.etree.ElementTree as ET
import csv
import sys

def normalize_license(license_name):
    """Normalize license names to SPDX identifiers"""
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
        'EPL-1.0': 'EPL-1.0'
    }
    
    return license_map.get(license_name, license_name)

def convert_xml_to_csv(xml_file, csv_file):
    tree = ET.parse(xml_file)
    root = tree.getroot()
    
    with open(csv_file, 'w', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(['Component', 'Origin', 'License', 'Copyright'])
        
        for dep in root.findall('.//dependency'):
            groupId = dep.find('groupId')
            artifactId = dep.find('artifactId') 
            version = dep.find('version')
            license_elem = dep.find('licenses/license/name')
            
            if groupId is not None and artifactId is not None and version is not None:
                component = 'sticker-award'
                origin = f'maven:{groupId.text}:{artifactId.text}:jar:{version.text}'
                license_name = license_elem.text if license_elem is not None else ''
                normalized_license = normalize_license(license_name)
                copyright = ''
                
                writer.writerow([component, origin, normalized_license, copyright])

if __name__ == '__main__':
    convert_xml_to_csv('sticker-award/target/generated-sources/license/licenses.xml', 'LICENSE-3rdparty.csv')