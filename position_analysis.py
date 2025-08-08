#!/usr/bin/env python3

"""
Position-based field analysis to understand the exact structure needed
"""

def analyze_expected_structure():
    print("=== POSITION-BASED FIELD ANALYSIS ===")
    
    # Expected output line from MBCNTR2053_Expected_Output
    expected_line = "5031|20061255|P|1|THIS IS A SAMPLE||||123 MY PLACES|HOWARD|FL|12345||2207|382||MY PLACES|HOWARD               FL12345 2207|12345 2207|2038043020|2038043020|211428773|0|125|8|1|0|0|0|125|4|1||||0|0|0|0|0|0|0|155|7|784.58|591.65|192.93|12.11|9.27|77.42|0.00|94.13|0.00|0.00|0.00|0.00|0.00|0.00|0.00|29.58|92400.00|1322.44|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|486.33|0.00|0.00|0.00|0.00|0.00|0.00|0.00|1322.44|0.00|0.00|0.00|0.00|0.00|1322.44|784.58|0.00|0.00||1|3|SR1|001||1|15|12|6.62500|7|MF|T|1|37|360|||||||||||||||||||||||N|||0.00|0.00|0.00|0.00|0.00|N|0.00|0||125|6|4|125|8|1||||||||0.00|0.00|0.0000000||0|0|0|0|0|0|0|0|1||0|0|0|0|0|0||0.00|||||||||0.00|||0|0||||||||||0|0||92400|0.00|0|0|0.00|0|0|0.00|0|0|0|0|0.00||||||||||||||||0|0000||0.00|||||||||||||0.00|0.00|||||||||||||||||||||||0|00|00|0|00|00|0.00000|81.53|0.00|510.12|0.00|0.00|0.00|0.00|0.00|N||0|0|0|N|0|0|0||125|8|1|125|8|16|0|0|0.00|0.00|0.00|0.00|0.00|0|0|0|0.00|0.00|0|0|0|0|0|0|0.00|0|0|0|0.00||||0|0|0||||||0|0|0|0|0|0|||0|0|0|0|0|0|0|0|0|0|0|0|0.00|||||0||0|00|00|0.00|0.00|0.00||0|00|00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0|00|00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00||0|LDROTAN@GMAIL.COM|0|00|00|0.00|0.00|0|00|00|0|00|00|0|00|00|0.00|0.00|0.00||0|00|00|0|00|00|0.0662500|0|00|00|0|00|00|||||0.00|0|00|00||0|00|00|0.00||0.00|||||||||||||||||||||||||||||||||||||||||||||||||0.00||0.00|||0|00|00|||||||0|00|00|0.00|0.00|0.00||0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00||92910.12|||||||||||||"
    
    fields = expected_line.split("|")
    print(f"Total fields in expected output: {len(fields)}")
    
    # Analyze key positions where we know specific values should appear
    key_positions = {
        140: "125",
        141: "6", 
        142: "4",
        143: "125",
        144: "8",
        145: "1"
    }
    
    print(f"\n=== KEY POSITION ANALYSIS ===")
    for pos, expected_value in key_positions.items():
        if pos - 1 < len(fields):  # Convert to 0-based index
            actual_value = fields[pos - 1]
            status = "✓" if actual_value == expected_value else "✗"
            print(f"Position {pos}: Expected='{expected_value}' | Actual='{actual_value}' {status}")
        else:
            print(f"Position {pos}: Expected='{expected_value}' | Actual=NOT_FOUND ✗")
    
    # Show the structure around position 140
    print(f"\n=== CONTEXT AROUND POSITION 140 ===")
    start = 135
    end = 150
    for i in range(start, min(end + 1, len(fields))):
        marker = " <-- TARGET" if i + 1 in key_positions else ""
        print(f"Position {i + 1:3d}: '{fields[i]}'{marker}")
    
    # Create a template for the exact structure needed
    print(f"\n=== FIELD STRUCTURE TEMPLATE ===")
    print("Based on analysis, here's what we need to generate:")
    
    # Group fields into logical sections
    sections = {
        "Header": (1, 22),
        "Financial": (23, 87), 
        "Loan Info": (88, 103),
        "Extended Start": (104, 127),
        "Key Sequence": (128, 145),  # This is where "N" and "125,6,4,125,8,1" should be
        "Remaining": (146, 533)
    }
    
    for section_name, (start_pos, end_pos) in sections.items():
        print(f"\n{section_name} (Positions {start_pos}-{end_pos}):")
        sample_fields = []
        for i in range(start_pos - 1, min(end_pos, len(fields))):
            sample_fields.append(fields[i])
        print(f"  Sample: {sample_fields[:10]}...")  # Show first 10 fields
        
    return fields

def create_position_mapping():
    """Create a position-to-value mapping for the expected output"""
    fields = analyze_expected_structure()
    
    print(f"\n=== CREATING POSITION MAPPING ===")
    
    # Create a dictionary mapping position to expected value
    position_map = {}
    for i, field_value in enumerate(fields, 1):
        position_map[i] = field_value
    
    # Focus on the critical positions we've been trying to fix
    critical_positions = range(120, 160)
    print(f"\nCritical positions {min(critical_positions)} to {max(critical_positions)}:")
    for pos in critical_positions:
        value = position_map.get(pos, "NOT_FOUND")
        print(f"  Position {pos:3d}: '{value}'")
    
    return position_map

if __name__ == "__main__":
    position_map = create_position_mapping()
