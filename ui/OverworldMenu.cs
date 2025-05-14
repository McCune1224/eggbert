using Godot;

public partial class OverworldMenu : Control
{
    private AnimationPlayer _animationPlayer;

    public override void _Ready()
    {
        HideMenu();
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // Connect button signals to methods
        GetNode("PanelContainer/VBoxContainer/ResumeButton")
            .Connect("pressed", new Callable(this, nameof(OnResumePressed)));
        GetNode("PanelContainer/VBoxContainer/InventoryButton")
            .Connect("pressed", new Callable(this, nameof(OnInventoryPressed)));
        GetNode("PanelContainer/VBoxContainer/SaveButton")
            .Connect("pressed", new Callable(this, nameof(OnSavePressed)));
        GetNode("PanelContainer/VBoxContainer/QuitButton")
            .Connect("pressed", new Callable(this, nameof(OnQuitPressed)));
    }

    public override void _Process(double delta)
    {
        ToggleEscape();
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


    private void ToggleEscape()
    {
        if (Input.IsActionJustPressed("menu_pause"))
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
        Node overworldNode = tree.GetNodesInGroup("overworld")[0];


        GameSaverLoader gsl = new(overworldNode.GetTree());
        gsl.SaveGame();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
