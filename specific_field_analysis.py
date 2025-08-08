#!/usr/bin/env python3

"""
Analyze specific field positions around the problem area
"""

def analyze_specific_fields():
    print("=== SPECIFIC FIELD ANALYSIS AROUND POSITION 126 ===")
    
    # Read the generated output (first record)
    with open("Output/69172p.asc", "r", encoding='utf-8', errors='replace') as f:
        generated_line = f.readline().strip()
    generated_fields = generated_line.split("|")
    
    # Read the expected output (first record)
    with open("../MBCNTR2053_Expected_Output/expected_p.txt", "r", encoding='utf-8', errors='replace') as f:
        expected_line = f.readline().strip()
    expected_fields = expected_line.split("|")
    
    print(f"Generated fields: {len(generated_fields)}")
    print(f"Expected fields: {len(expected_fields)}")
    
    # Show fields around position 126
    print(f"\n=== CONTEXT AROUND FIELD 126 ===")
    start = 120
    end = 135
    
    print("Field# | Generated | Expected")
    print("-------|-----------|----------")
    for i in range(start, min(end, len(generated_fields), len(expected_fields))):
        gen_val = generated_fields[i] if i < len(generated_fields) else "N/A"
        exp_val = expected_fields[i] if i < len(expected_fields) else "N/A"
        diff_marker = " <-- DIFF" if gen_val != exp_val else ""
        print(f"{i+1:6} | {gen_val:9} | {exp_val:9}{diff_marker}")
    
    # Look for where the "N" value should actually be
    print(f"\n=== SEARCHING FOR THE 'N' VALUE ===")
    for i in range(120, 140):
        if i < len(expected_fields) and expected_fields[i] == "N":
            print(f"Expected 'N' found at field {i+1}")
        if i < len(generated_fields) and generated_fields[i] == "N":
            print(f"Generated 'N' found at field {i+1}")

if __name__ == "__main__":
    analyze_specific_fields()
