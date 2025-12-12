using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cooney.AI.ComfyUI;

public class ComfyUIImageGenerationOptions(Workflow workflow) : ImageGenerationOptions
{
	public Workflow? Workflow { get; set; } = workflow;
}
