from kivy.uix.screenmanager import Screen

class MenuScreen(Screen):
    def start_game(self):
        self.manager.current = "game"
