using Godot;
using System;
using System.Collections.Generic;

public partial class GrandpaSmith : StaticBody2D
{
	// Reference to a label that will show the interaction prompt
	private Label dialogueLabel;
	private AudioStream speechSound = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/meep.mp3");
	private ComponentPromptCollision promptCollision;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Add the label as a child of this node
		dialogueLabel = new Label();
		AddChild(dialogueLabel);
		promptCollision = GetNode<ComponentPromptCollision>("ComponentPromptCollision");
	}


	// Called when a physics body exits this Area2D
	private void OnBodyExited(Node2D body)
	{
		// Check if the body that left is the player
		if (body.IsInGroup("player")) // Adjust this to match your player node name
		{
			// Hide the interaction prompt
			dialogueLabel.Visible = false;
			DialogManager.Instance.Reset();
		}
	}


	public override void _Input(InputEvent @event)
	{
		// Check for the 'E' key press when the prompt is visible
		if (promptCollision.isPromptVisible() && Input.IsActionJustPressed("interact"))
		{
			promptCollision.HidePrompt();

			List<string> dialog = new(); ;
			dialog.Add("This is the first item. Very Cool");
			dialog.Add("Item Two! Okay, not much to see here...");
			dialog.Add("What the fuck did you just fucking say about me, you little bitch? I'll have you know I graduated top of my class in the Navy Seals, and I've been involved in numerous secret raids on Al-Quaeda, and I have over 300 confirmed kills. I am trained in gorilla warfare and I'm the top sniper in the entire US armed forces. You are nothing to me but just another target. I will wipe you the fuck out with precision the likes of which has never been seen before on this Earth, mark my fucking words. You think you can get away with saying that shit to me over the Internet? Think again, fucker. As we speak I am contacting my secret network of spies across the USA and your IP is being traced right now so you better prepare for the storm, maggot. The storm that wipes out the pathetic little thing you call your life. You're fucking dead, kid. I can be anywhere, anytime, and I can kill you in over seven hundred ways, and that's just with my bare hands. Not only am I extensively trained in unarmed combat, but I have access to the entire arsenal of the United States Marine Corps and I will use it to its full extent to wipe your miserable ass off the face of the continent, you little shit. If only you could have known what unholy retribution your little 'clever' comment was about to bring down upon you, maybe you would have held your fucking tongue. But you couldn't, you didn't, and now you're paying the price, you goddamn idiot. I will shit fury all over you and you will drown in it. You're fucking dead, kiddo.");
			DialogManager.Instance.StartDialog(dialog, speechSound);
			// PackedScene tb = ResourceLoader.Load<PackedScene>("res://ui/TextBox.tscn");
			// TextBox cb = tb.Instantiate<TextBox>();
			// GetTree().Root.AddChild(cb);
			// GD.Print("TODO: ", cb);
			// cb.DisplayText(dialog[0]);

			// // This is where you'd put the code for what happens when the player interacts
			// // For example: Start a dialogue, give an item, etc.
			// dialogueLabel.Text = "Hello, young one! You look yoked out..."; // Set the dialogue text
			// dialogueLabel.Position = new Vector2(0, -50); // Position above the NPC
			// dialogueLabel.HorizontalAlignment = HorizontalAlignment.Left;
			//
			// interactionPrompt.Visible = false;
			// dialogueLabel.Visible = true;
			// //show the dialogueLabel
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
