#!/usr/bin/env python3
"""
Field-by-field analysis tool for comparing generated vs expected output
"""

def analyze_records():
    # Read generated output
    with open(r"Output\69172p.asc", "r") as f:
        generated_lines = f.readlines()
    
    # Read expected output
    with open(r"..\MBCNTR2053_Expected_Output\expected_p.txt", "r") as f:
        expected_lines = f.readlines()
    
    print("=== RECORD-BY-RECORD FIELD ANALYSIS ===\n")
    
    for record_idx in range(min(len(generated_lines), len(expected_lines))):
        generated_record = generated_lines[record_idx].strip()
        expected_record = expected_lines[record_idx].strip()
        
        generated_fields = generated_record.split('|')
        expected_fields = expected_record.split('|')
        
        print(f"RECORD {record_idx + 1} ANALYSIS:")
        print(f"Generated fields: {len(generated_fields)}")
        print(f"Expected fields: {len(expected_fields)}")
        print()
        
        # Compare ALL fields
        max_fields = max(len(generated_fields), len(expected_fields))
        
        differences = []
        for field_idx in range(max_fields):
            gen_field = generated_fields[field_idx] if field_idx < len(generated_fields) else "MISSING"
            exp_field = expected_fields[field_idx] if field_idx < len(expected_fields) else "MISSING"
            
            if gen_field != exp_field:
                differences.append({
                    'field': field_idx + 1,
                    'generated': gen_field,
                    'expected': exp_field
                })
        
        if differences:
            print("DIFFERENCES FOUND:")
            for diff in differences[:20]:  # Show first 20 differences
                print(f"  Field {diff['field']:3d}: Generated='{diff['generated']}' | Expected='{diff['expected']}'")
            if len(differences) > 20:
                print(f"  ... and {len(differences) - 20} more differences")
        else:
            print("ALL 533 fields match perfectly!")
        
        print("-" * 80)
        print()

if __name__ == "__main__":
    analyze_records()
