   M S B A M L   `   `   `  ����  MultronWinCleaner$    Multron_Win_Cleaner.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Multron Win Cleaner��5   	   $	��500��6%   $	��600��62    � WindowStyle$
 None=�5   	    � 
ResizeMode$ CanResizeWithGrip=�6    � AllowsTransparency$
 True��6;    � WindowStartupLocation$ CenterScreen=�6U   ��5      C� 5	   
    C� ResourceKey$ WindowBackground��6   .��5      � .+�5   
   �� -   5           ButtonHoverBackground#���   5       �� Padding$ 2q�6F   $	��Top=�6R   $��Stretch=�6j   ._�5      �  � ColumnDefinitions 5      �� 5      $��* �6'   �� 5      $
��Auto �6'   �� 5      $
��Auto �6'   �� 5      $
��Auto �6'   .+�5      ��  �� Orientation$ 
Horizontal=�6   $��Center=�66   $
��Left=�6Q   $	��5,0q�6l   .+�5      �� $��Multron Win Cleaner��6    $��14(�6;   $
��Bold"�6I   
�������6[   �� 5      $��v1.8.1 beta��6    $��8(�63   
�������$��Center=�6S   $
��Left=�6n   $	��3,0q�6�   5      �� 5      -   5        $��-��5      $��12(�6%   $
��Bold"�63   
�������
������ 
������ $��1��5   6   $��30��6F   $��20��6Q   $��0,0,0,0q�6]   �� 5       -   5        	 ��Name$	 buttonMaximize��5       $	��↔��61   $��12(�6=   $
��Bold"�6K   
�������
������ 
������ $��2��5!   6   $��30��6F   $��20��6Q   $��0,0,0,0q�6]   �� 5#      -   5        $��X��5#      $��12(�6%   $
��Bold"�63   
����������#B61423
������ $��3��5$   3   $��30��6C   $��20��6N   $��0,0,0,0q�6Z   5%      5&      � 5)   
   $��	0,70,0,56q�6   
 � RowDefinitions
 5*      �� 5+      $
��Auto �6    �� 5,      $
��Auto �6    �� 5-      $
��Auto �6    �� 5.      $	��19* �6    �� 5/      $	��75* �6    �� 50      $	��38* �6    .+�53      �� $��0��6   $ 
Horizontal=�6&   $��Center=�6?   $��	0,10,0,10q�6\   .+�54      �� -   5        $	 buttonStartScan��54      $
��Scan��62     ModernButtonStyle#���  6A   6m   �� 55      -   5        $	 buttonCancel��55      $��Cancel��6/   #���  6@   $��10,0,0,0q�6�   56      � 58      -   5         �Name$ progressBar1��58       � Minimum$ 0Q�60    � Maximum$	 100Q�6<    � Value$ 0Q�6J   $��15��6T   ���#D5DBDB���#0061A8���#A6A6A6$��1q�5:      $��
38,10,39,0q�6.   $	��Top=�6B   $��1��6Z   �� 5<      -   5         ��Name$ label1_Copy��5<      $��Scanning...��6)   $��2��6?   $	��Top=�6L   $
��Left=�6d   $��33,5,0,0q�6   $��33��6�   $��2��6�     Text#���  6�   �� 5>      -	   5         ��Name$ ScrollViewer��5>      $��3��61   $
��Auto=�6>   $
��Auto=�6a   $��33,36,39,-6q�6�   $��3��6�   .��5?      � -
   5         �Name$ 
wrapPanel1��5?      $
��Left=�60   $	��Top=�6K    � Orientation$ Vertical=�6c   5@      �� 5B      -   5        $ ScrollViewerDirectories��5B      $��4��6<   $
��Auto=�6I   $
��Auto=�6l   $��33,0,27,-20q�6�   $��2��6�    J� RenderTransformOrigin$ 0.5,0.5&�6�   z�5C      V� 5D      .~�5E      �� �� 5F       �� AngleY$ 0Q�6(   �� 5G      T� 5H       T� Y$ 0Q�6-   5I      .��5K      � -   5        $ wrapPanelDirectories��5K      $
��Left=�6:   $	��Top=�6U   $ Vertical=�6m   5L      �� 5M      -   5         ��Name$ 	CheckBox1��5M      $
��Dark��6*   $
��Left=�69   $��
10,-40,0,0q�6T   $	��Top=�6h   #���  6�   5N      5O      5P      