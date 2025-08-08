#!/usr/bin/env python3
"""
Detailed record-by-record comparison between expected and actual MB2000 output.
This script provides comprehensive analysis of field alignment and value differences.
"""

import os
import sys
from typing import List, Tuple, Dict

def read_records(file_path: str) -> List[str]:
    """Read all records from a file."""
    if not os.path.exists(file_path):
        print(f"File not found: {file_path}")
        return []
    
    with open(file_path, 'r', encoding='utf-8') as f:
        return [line.strip() for line in f if line.strip()]

def parse_record(record: str) -> Tuple[str, List[str]]:
    """Parse a record into record type and fields."""
    fields = record.split('|')
    if len(fields) >= 3:
        record_type = fields[2]  # A, D, P, S
        return record_type, fields
    return 'UNKNOWN', fields

def compare_fields(our_fields: List[str], expected_fields: List[str], record_type: str) -> Dict:
    """Compare two field arrays and return detailed analysis."""
    max_fields = max(len(our_fields), len(expected_fields))
    
    comparison = {
        'record_type': record_type,
        'our_field_count': len(our_fields),
        'expected_field_count': len(expected_fields),
        'matches': 0,
        'mismatches': [],
        'missing_fields': [],
        'extra_fields': []
    }
    
    # Compare common fields
    for i in range(min(len(our_fields), len(expected_fields))):
        if our_fields[i] == expected_fields[i]:
            comparison['matches'] += 1
        else:
            comparison['mismatches'].append({
                'field_index': i + 1,
                'our_value': our_fields[i],
                'expected_value': expected_fields[i]
            })
    
    # Check for missing fields (expected has more)
    if len(expected_fields) > len(our_fields):
        for i in range(len(our_fields), len(expected_fields)):
            comparison['missing_fields'].append({
                'field_index': i + 1,
                'expected_value': expected_fields[i]
            })
    
    # Check for extra fields (we have more)
    if len(our_fields) > len(expected_fields):
        for i in range(len(expected_fields), len(our_fields)):
            comparison['extra_fields'].append({
                'field_index': i + 1,
                'our_value': our_fields[i]
            })
    
    return comparison

def analyze_primary_record(our_primary: str, expected_primary: str) -> Dict:
    """Detailed analysis of Primary (P) record."""
    our_type, our_fields = parse_record(our_primary)
    expected_type, expected_fields = parse_record(expected_primary)
    
    print(f"\n=== PRIMARY RECORD ANALYSIS ===")
    print(f"Our record type: {our_type}, Expected: {expected_type}")
    print(f"Our field count: {len(our_fields)}, Expected: {len(expected_fields)}")
    
    comparison = compare_fields(our_fields, expected_fields, 'P')
    
    # Critical field analysis (first 50 fields)
    print(f"\n--- First 50 Fields Analysis ---")
    critical_mismatches = 0
    for i in range(min(50, len(our_fields), len(expected_fields))):
        if our_fields[i] != expected_fields[i]:
            critical_mismatches += 1
            print(f"Field {i+1:2d}: Our='{our_fields[i]:15s}' Expected='{expected_fields[i]:15s}'")
    
    print(f"Critical field mismatches (first 50): {critical_mismatches}")
    
    # Field ranges analysis
    print(f"\n--- Field Range Analysis ---")
    ranges = [
        (1, 20, "Header and Identifiers"),
        (21, 50, "Address and Contact"),
        (51, 100, "Financial Data 1"),
        (101, 150, "Financial Data 2"),
        (151, 200, "System Fields 1"),
        (201, 300, "Extended Fields"),
        (301, 400, "Compliance Fields"),
        (401, 500, "Additional Fields"),
        (501, 533, "Trailing Fields")
    ]
    
    for start, end, description in ranges:
        range_matches = 0
        range_mismatches = 0
        for i in range(start-1, min(end, len(our_fields), len(expected_fields))):
            if our_fields[i] == expected_fields[i]:
                range_matches += 1
            else:
                range_mismatches += 1
        
        total_range = min(end, len(our_fields), len(expected_fields)) - (start-1)
        if total_range > 0:
            match_pct = (range_matches / total_range) * 100
            print(f"{description:20s} ({start:3d}-{end:3d}): {range_matches:3d}/{total_range:3d} matches ({match_pct:5.1f}%)")
    
    return comparison

def main():
    """Main comparison function."""
    # File paths
    expected_file = r"c:\Users\Shan\Documents\Legacy Mordernization\MBCNTR2053_Expected_Output\expected_p.txt"
    our_file = r"c:\Users\Shan\Documents\Legacy Mordernization\LegacyModernizationPoC\Output\69172p.asc"
    
    print("=== MB2000 CONVERSION DETAILED COMPARISON ===")
    print(f"Expected file: {expected_file}")
    print(f"Our file: {our_file}")
    
    # Read expected output
    if not os.path.exists(expected_file):
        print(f"Expected file not found: {expected_file}")
        return
    
    with open(expected_file, 'r', encoding='utf-8') as f:
        expected_content = f.read().strip()
    
    # Read our output
    our_records = read_records(our_file)
    if not our_records:
        print("No records found in our output file")
        return
    
    print(f"\nRecord counts:")
    print(f"Our output: {len(our_records)} records")
    print(f"Expected: 1 primary record")
    
    # Find Primary record in our output
    our_primary = None
    for record in our_records:
        record_type, fields = parse_record(record)
        if record_type == 'P':
            our_primary = record
            break
    
    if not our_primary:
        print("No Primary (P) record found in our output!")
        return
    
    # Compare Primary records
    analysis = analyze_primary_record(our_primary, expected_content)
    
    # Summary
    print(f"\n=== SUMMARY ===")
    print(f"Total field comparison:")
    print(f"  Our fields: {analysis['our_field_count']}")
    print(f"  Expected fields: {analysis['expected_field_count']}")
    print(f"  Matching fields: {analysis['matches']}")
    print(f"  Mismatched fields: {len(analysis['mismatches'])}")
    print(f"  Missing fields: {len(analysis['missing_fields'])}")
    print(f"  Extra fields: {len(analysis['extra_fields'])}")
    
    if analysis['expected_field_count'] > 0:
        match_percentage = (analysis['matches'] / analysis['expected_field_count']) * 100
        print(f"  Match percentage: {match_percentage:.1f}%")
    
    # Show critical mismatches (first 20)
    if analysis['mismatches']:
        print(f"\n--- Critical Mismatches (first 20) ---")
        for i, mismatch in enumerate(analysis['mismatches'][:20]):
            print(f"Field {mismatch['field_index']:3d}: '{mismatch['our_value']:20s}' != '{mismatch['expected_value']:20s}'")
    
    # Show field differences at specific positions
    print(f"\n--- Address Field Analysis (Fields 10-15) ---")
    our_type, our_fields = parse_record(our_primary)
    expected_type, expected_fields = parse_record(expected_content)
    
    for i in range(9, min(15, len(our_fields), len(expected_fields))):
        status = "✓" if our_fields[i] == expected_fields[i] else "✗"
        print(f"{status} Field {i+1:2d}: Our='{our_fields[i]:30s}' Expected='{expected_fields[i]:30s}'")

if __name__ == "__main__":
    main()
