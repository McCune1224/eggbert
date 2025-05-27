using Godot;

public partial class OverworldMenu : CanvasLayer
{
    private AnimationPlayer _animationPlayer;
    private Button _resumeButton;
    private Button _inventoryButton;
    private Button _saveButton;
    private Button _loadButton;
    private Button _quitButton;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // Connect button signals to methods
        // GetNode("PanelContainer/VBoxContainer/ResumeButton")
        //     .Connect("pressed", new Callable(this, nameof(OnResumePressed)));
        // GetNode("PanelContainer/VBoxContainer/InventoryButton")
        //     .Connect("pressed", new Callable(this, nameof(OnInventoryPressed)));
        // GetNode("PanelContainer/VBoxContainer/SaveButton")
        //     .Connect("pressed", new Callable(this, nameof(OnSavePressed)));
        // GetNode("PanelContainer/VBoxContainer/QuitButton")
        //     .Connect("pressed", new Callable(this, nameof(OnQuitPressed)));

        _resumeButton = GetNode<Button>("PanelContainer/VBoxContainer/ResumeButton");
        _resumeButton.Connect("pressed", new Callable(this, nameof(OnResumePressed)));

        _inventoryButton = GetNode<Button>("PanelContainer/VBoxContainer/InventoryButton");
        _inventoryButton.Connect("pressed", new Callable(this, nameof(OnInventoryPressed)));

        _saveButton = GetNode<Button>("PanelContainer/VBoxContainer/SaveButton");
        _saveButton.Connect("pressed", new Callable(this, nameof(OnSavePressed)));

        _saveButton = GetNode<Button>("PanelContainer/VBoxContainer/LoadButton");
        _saveButton.Connect("pressed", new Callable(this, nameof(OnLoadPressed)));

        _quitButton = GetNode<Button>("PanelContainer/VBoxContainer/QuitButton");
        _quitButton.Connect("pressed", new Callable(this, nameof(OnQuitPressed)));

        HideMenu();
    }


    public override void _Process(double delta)
    {
    }

    private void Resume()
    {
        var sceneTree = GetTree();
        sceneTree.Paused = false;
        HideMenu();
    }

    private void Pause()
    {
        var sceneTree = GetTree();
        sceneTree.Paused = true;
        ShowMenu();
    }

    private void ShowMenu()
    {
        Visible = true;
        if (_animationPlayer != null && _animationPlayer.HasAnimation("show_menu"))
        {
            _animationPlayer.Play("show_menu");
        }
        _resumeButton.GrabFocus();
    }

    private void HideMenu()
    {
        if (_animationPlayer != null && _animationPlayer.HasAnimation("hide_menu"))
        {
            _animationPlayer.Play("hide_menu");
        }
        else
        {
            Visible = false;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("menu_pause")) { ToggleEscape(); }
    }

    private void ToggleEscape()
    {
        {
            var sceneTree = GetTree();
            if (sceneTree.Paused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // Button handlers
    private void OnResumePressed()
    {
        Resume();
    }

    private void OnInventoryPressed()
    {
        // Add inventory logic here
    }

    private void OnSavePressed()
    {
        SceneTree tree = GetTree();
        SaveLoadManager.Instance.SaveGame();

    }

    private void OnLoadPressed()
    {
        SceneTree tree = GetTree();
        SaveLoadManager.Instance.LoadGame();
        Resume();
    }



    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
