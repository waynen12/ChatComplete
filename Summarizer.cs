
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;


public class Summarizer{

	public TextToSummarize TextToSummarize {get;private set;}
	public bool SuccessfullySummarized {get;private set;}
	public string Summary {get;set;}
	public bool PreviouslySet{get;set;}
	
	public Summarizer(TextToSummarize text)
	{
		TextToSummarize = text;
		//...
	}
	
	[KernelFunction("summarize_text")]
	[Description("Summarize th etext given into exactly seven words")]
	[return: Description("String with 7 words")]
	public string SummarizeText(string textToSummarize){
		
		var words = textToSummarize.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(7);
		return string.Join(" ", words);
	}

	[KernelFunction("get_summarization_status")]
	[Description("Get the status of summarization contained in the summarize object")]
	[return: Description("Summarize object")]
	public Summarizer GetTheSummarizationStatus(TextToSummarize text){
		TextToSummarize = text;
		Summary = SummarizeText(text.Content);
		SuccessfullySummarized = true;
		return this;
	}
}
public class TextToSummarize{
	
	public string Content {get;private set;}
	public int Length => Content.Length;
	public TextToSummarize(string content)
	{
		Content = content;
	}
}