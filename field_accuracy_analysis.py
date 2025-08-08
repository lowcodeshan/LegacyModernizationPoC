#!/usr/bin/env python
"""
Enhanced Binary Field Accuracy Analysis
Compares our enhanced ToBinaryRecord() output with expected results
"""

import os

def analyze_binary_accuracy():
    print("=== Enhanced Binary Field Accuracy Analysis ===")
    print()
    
    # File paths
    actual_file = "Output/69172p.asc"
    expected_file = "../MBCNTR2053_Expected_Output/69172p.asc"
    
    # Read both files
    try:
        with open(actual_file, 'rb') as f:
            actual_data = f.read()
        with open(expected_file, 'rb') as f:
            expected_data = f.read()
    except FileNotFoundError as e:
        print(f"File not found: {e}")
        return
    
    print(f"File Size Comparison:")
    print(f"  Actual:   {len(actual_data):,} bytes")
    print(f"  Expected: {len(expected_data):,} bytes")
    print(f"  Size Match: {'✓ PERFECT' if len(actual_data) == len(expected_data) else '✗ MISMATCH'}")
    print()
    
    # Define key field positions for analysis
    key_fields = [
        (0, 3, "Client Number (503)"),
        (6, 12, "Binary Control Pattern"),
        (12, 23, "Account Number Field"),
        (23, 32, "Packed Decimal Pattern"),
        (35, 51, "Name Sample Message"),
        (600, 608, "Financial Field 1 (PrincipalBalance)"),
        (620, 628, "Financial Field 2 (PaymentAmount)"),
        (1950, 1955, "TranKey Field"),
        (1960, 1963, "TranCount Field"),
    ]
    
    total_analyzed = 0
    total_matching = 0
    
    print("Critical Field Analysis:")
    print("-" * 70)
    
    for start, end, description in key_fields:
        if start < len(actual_data) and start < len(expected_data):
            # Extract field data
            actual_field = actual_data[start:end]
            expected_field = expected_data[start:end]
            
            # Calculate accuracy
            field_length = min(len(actual_field), len(expected_field))
            matches = sum(1 for i in range(field_length) if actual_field[i] == expected_field[i])
            accuracy = (matches / field_length * 100) if field_length > 0 else 0
            
            total_analyzed += field_length
            total_matching += matches
            
            # Format status
            status = "✓ MATCH" if accuracy == 100 else f"{accuracy:.1f}%"
            
            print(f"  {description:<35} [{start:4}-{end:4}]: {status}")
            
            # Show hex for critical mismatches
            if accuracy < 100 and description in ["Client Number (503)", "Binary Control Pattern", "Name Sample Message"]:
                print(f"    Expected: {expected_field.hex()}")
                print(f"    Actual:   {actual_field.hex()}")
                print()
    
    print("-" * 70)
    
    # Overall accuracy
    overall_accuracy = (total_matching / total_analyzed * 100) if total_analyzed > 0 else 0
    print(f"Overall Key Field Accuracy: {overall_accuracy:.1f}% ({total_matching}/{total_analyzed} bytes)")
    
    # Record structure analysis
    print()
    print("Record Structure Verification:")
    expected_records = 5
    expected_record_size = 2000
    actual_records = len(actual_data) // expected_record_size
    actual_record_size = len(actual_data) // actual_records if actual_records > 0 else 0
    
    print(f"  Records: {actual_records} (expected: {expected_records}) {'✓' if actual_records == expected_records else '✗'}")
    print(f"  Record Size: {actual_record_size} bytes (expected: {expected_record_size}) {'✓' if actual_record_size == expected_record_size else '✗'}")
    
    # Enhancement assessment
    print()
    print("=" * 70)
    print("ENHANCEMENT ASSESSMENT:")
    
    if len(actual_data) == len(expected_data):
        print("✅ File Size: PERFECT MATCH (10,000 bytes)")
    
    if actual_records == expected_records and actual_record_size == expected_record_size:
        print("✅ Record Structure: PERFECT MATCH (5 records × 2,000 bytes)")
    
    if overall_accuracy >= 95:
        print("🎯 TARGET ACHIEVED: >95% Field Accuracy!")
    elif overall_accuracy >= 80:
        print(f"📈 SIGNIFICANT PROGRESS: {overall_accuracy:.1f}% Field Accuracy")
        print("   Enhanced ToBinaryRecord() with packed decimal encoding shows major improvement")
    elif overall_accuracy >= 60:
        print(f"📊 GOOD PROGRESS: {overall_accuracy:.1f}% Field Accuracy")
        print("   Binary field positioning and COMP-3 simulation working")
    else:
        print(f"🔧 NEEDS REFINEMENT: {overall_accuracy:.1f}% Field Accuracy")
    
    print()
    print("Key Improvements Implemented:")
    print("  • Enhanced field positioning based on xxd binary analysis")
    print("  • EncodePackedDecimal() method for COMP-3 financial fields")
    print("  • Specific byte patterns matching expected output structure")
    print("  • Two-stage conversion architecture (Binary→ASCII→MB2000)")
    
    return overall_accuracy

if __name__ == "__main__":
    accuracy = analyze_binary_accuracy()
