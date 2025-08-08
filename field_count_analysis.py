#!/usr/bin/env python3

"""
Detailed field counting analysis for each section of the output
"""

def analyze_field_counts():
    print("=== FIELD COUNT ANALYSIS BY SECTION ===")
    
    # Count fields in each section based on the code
    
    # Initial fields (before AddFinancialFields)
    print("\n1. INITIAL FIELDS (Lines before AddFinancialFields):")
    initial_fields = [
        "Acct_Num", "''", "BorrFirstName", "BorrLastName", "''", 
        "BillAdd1", "BillAdd2", "''", "''", "BillCity", 
        "BillState", "Zip5", "''", "TeleNo", "SecTeleNo", 
        "''", "MY PLACES", "formatted_address", "zip_phone", "2038043020", 
        "2038043020", "211428773", "0"
    ]
    print(f"Initial fields count: {len(initial_fields)}")
    for i, field in enumerate(initial_fields, 1):
        print(f"  Field {i}: {field}")
    
    # Financial fields
    print("\n2. FINANCIAL FIELDS (AddFinancialFields):")
    financial_fields = [
        "125", "8", "1", "0", "0", "0", "125", "4", "1", "", "", "",
        "0", "0", "0", "0", "0", "0", "0", "155", "7",
        "784.58", "591.65", "192.93", "12.11", "9.27", "77.42", "0.00", "94.13", "0.00", "0.00",
        "0.00", "0.00", "0.00", "0.00", "0.00", "29.58", "92400.00", "1322.44", "0.00", "0.00",
        "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "486.33", "0.00", "0.00",
        "0.00", "0.00", "0.00", "0.00", "0.00", "1322.44", "0.00", "0.00", "0.00", "0.00",
        "0.00", "1322.44", "784.58", "0.00", "0.00"
    ]
    print(f"Financial fields count: {len(financial_fields)}")
    
    # Loan characteristic fields
    print("\n3. LOAN CHARACTERISTIC FIELDS (AddLoanCharacteristicFields):")
    loan_fields = [
        "", "LoanProgram", "LoanType", "ProgramCode", "ProgramSubCode", "",
        "1", "15", "12", "InterestRate", "LTV", "OccupancyCode", "PropertyType",
        "1", "TermRemaining", "OriginalTerm"
    ]
    print(f"Loan characteristic fields count: {len(loan_fields)}")
    
    # Extended fields - first 20 empty fields
    print("\n4. EXTENDED FIELDS - First empty fields:")
    first_empty_fields = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""]
    print(f"First empty fields count: {len(first_empty_fields)}")
    
    # Extended fields - main data section
    print("\n5. EXTENDED FIELDS - Main data section:")
    main_extended = [
        "N", "", "", "0.00", "0.00", "0.00", "0.00", "0.00", "N", "0.00", "0", "", "",
        "125", "6", "4", "125", "8", "1", "", "", "", "", "", "", "", "",
        "0.00", "0.00", "0.0000000", "", "", "0", "0", "0", "0", "0", "0", "0", "0", "1", "", "",
        "0", "0", "0", "0", "0", "0", "", "", "0.00", "", "", "", "", "", "", "", "", "", "", "", "", "",
        "0", "0", "0", "", "", "", "", "", "", "", "0", "0", "0", "0", "0", "0", "", "", "", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0.00"
    ]
    print(f"Main extended section count: {len(main_extended)}")
    
    # Calculate totals
    total_before_extended = len(initial_fields) + len(financial_fields) + len(loan_fields)
    total_extended = len(first_empty_fields) + len(main_extended)
    
    print(f"\n=== TOTALS ===")
    print(f"Initial + Financial + Loan: {total_before_extended}")
    print(f"Extended fields: {total_extended}")
    print(f"Grand total: {total_before_extended + total_extended}")
    print(f"Expected: 533")
    print(f"Difference: {533 - (total_before_extended + total_extended)}")
    
    # Show where field 126 would be
    print(f"\n=== FIELD 126 LOCATION ===")
    print(f"Field 126 would be at position {126} in the combined array")
    print(f"This falls in the extended fields section")
    print(f"Extended fields start at position {total_before_extended + 1}")
    print(f"So field 126 is at position {126 - total_before_extended} in extended fields")

if __name__ == "__main__":
    analyze_field_counts()
