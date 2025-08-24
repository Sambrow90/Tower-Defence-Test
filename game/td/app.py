from kivy.app import App
from kivy.lang import Builder
from kivy.uix.screenmanager import ScreenManager, SlideTransition
from kivy.core.window import Window
from td.util.resources import resource_path

class RootScreens(ScreenManager):
    pass

class RandomTDApp(App):
    title = "Modular TD (Kivy)"

    def build(self):
        # Window size default
        if Window.width < 1200 or Window.height < 720:
            Window.size = (1280, 720)

        # Load KV files
        Builder.load_file(resource_path("ui", "menu.kv"))
        Builder.load_file(resource_path("ui", "game.kv"))

        from td.screens.menu import MenuScreen   # noqa
        from td.screens.game import GameScreen   # noqa

        root = RootScreens(transition=SlideTransition())
        root.add_widget(MenuScreen(name="menu"))
        root.add_widget(GameScreen(name="game"))
        root.current = "menu"
        return root
