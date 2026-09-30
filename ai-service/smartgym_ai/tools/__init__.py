from smartgym_ai.tools.base_tool import ToolBase, ToolResult
from smartgym_ai.tools.registry import ToolRegistry, tool_registry
from smartgym_ai.tools.safety_tools import (
    ModerateTextTool,
    ValidateTicketTool,
    ValidateEquipmentTool,
    ValidatePriorityTool,
    ValidateRepairAmountTool,
    ValidateRequiredFieldsTool,
)

# Register safety tools in singleton registry
tool_registry.register(ModerateTextTool())
tool_registry.register(ValidateTicketTool())
tool_registry.register(ValidateEquipmentTool())
tool_registry.register(ValidatePriorityTool())
tool_registry.register(ValidateRepairAmountTool())
tool_registry.register(ValidateRequiredFieldsTool())

__all__ = [
    "ToolBase",
    "ToolResult",
    "ToolRegistry",
    "tool_registry",
    "ModerateTextTool",
    "ValidateTicketTool",
    "ValidateEquipmentTool",
    "ValidatePriorityTool",
    "ValidateRepairAmountTool",
    "ValidateRequiredFieldsTool",
]
