#!/usr/bin/env python3
"""
Detailed field-by-field analysis tool to find the exact shift point
"""

def find_shift_point():
    # Read generated output
    with open(r"Output\69172p.asc", "r") as f:
        generated_record = f.readline().strip()
    
    # Read expected output
    with open(r"..\MBCNTR2053_Expected_Output\expected_p.txt", "r") as f:
        expected_record = f.readline().strip()
    
    generated_fields = generated_record.split('|')
    expected_fields = expected_record.split('|')
    
    print("=== DETAILED FIELD SHIFT ANALYSIS ===\n")
    print(f"Generated fields: {len(generated_fields)}")
    print(f"Expected fields: {len(expected_fields)}\n")
    
    # Find the first difference
    first_diff = None
    for i in range(min(len(generated_fields), len(expected_fields))):
        if generated_fields[i] != expected_fields[i]:
            first_diff = i + 1
            break
    
    if first_diff:
        print(f"FIRST DIFFERENCE at field {first_diff}")
        
        # Show context around the first difference
        start = max(0, first_diff - 5)
        end = min(len(generated_fields), first_diff + 10)
        
        print("\nCONTEXT AROUND FIRST DIFFERENCE:")
        print("Field# | Generated | Expected")
        print("-------|-----------|----------")
        
        for i in range(start, end):
            gen_val = generated_fields[i] if i < len(generated_fields) else "MISSING"
            exp_val = expected_fields[i] if i < len(expected_fields) else "MISSING"
            marker = " <-- DIFF" if gen_val != exp_val else ""
            print(f"{i+1:6d} | {gen_val:9s} | {exp_val:9s}{marker}")
        
        # Try to find alignment by looking ahead
        print(f"\nLOOKING FOR ALIGNMENT...")
        print("Checking if generated data appears shifted...")
        
        # Check if generated field appears in expected fields nearby
        for offset in range(-3, 4):
            if offset == 0:
                continue
            check_idx = first_diff - 1 + offset
            if 0 <= check_idx < len(expected_fields):
                if generated_fields[first_diff - 1] == expected_fields[check_idx]:
                    print(f"Generated field {first_diff} matches expected field {check_idx + 1} (offset: {offset})")
    else:
        print("ALL FIELDS MATCH PERFECTLY!")

if __name__ == "__main__":
    find_shift_point()
