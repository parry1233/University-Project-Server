   M S B A M L   `   `   `  ����H  DCCU2.0_Server, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null/clr-namespace:CCU2._0_ServerCCU2._0_Server  5   	       CCU2._0_Server.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   .localclr-namespace:CCU2._0_Server     5   	   J FWpfAnimatedGif, Version=1.4.18.0, Culture=neutral, PublicKeyToken=null,gif"http://wpfanimatedgif.codeplex.com  5      -   5          � Title$  Server��5	   	   $��625.561��6   $��731.6��6)    � Icon$ Icon/igo_black.png��67   .��5
      � ���	#FF3F3F46 � ColumnDefinitions 5      �� 5      $
��380* �6   �� 5      $
��179* �6   �� 5      $
��234* �6   .+�5      �� -   5        $��ServerIPLabel��5   
   $	��IP:��6   $
��Left=�6-   $��125.8,61,0,0q�6H   $	��Top=�6^   
�������$��1��6�   $��2��6�   $	��123��6�   �� 5      -   5        $��ServerPortLabel��5   
   $��Port:��6!   $
��Left=�61   $��89.8,61,0,0q�6L   $	��Top=�6a   
�������$��2��6�   $��25��6�   $��78��6�   �� 5      -   5        $��ClientComboBox��5      $
��Left=�6#   $��125.8,91,0,0q�6>   $	��Top=�6T   $	��219��6l   $��2��6x   $��26��6�   $��1��6�   �� 5      -   5        $��SendTextBox��5      $
��Left=�6   $	��235��6:   $��125.8,146,0,0q�6G    �� TextWrapping$
 Wrap=�6^   $	��Top=�6r   $	��219��6�   $��2��6�   $��1��6�   �� 5      -   5        $��SendBTN��5      $��SEND MESSAGE��6   $
��Left=�61   $��125.6,386,0,0q�6L   $	��Top=�6c   $	��219��6{   $��41��6�   $��1��6�   $��16(�6�   $��	Agency FB)�6�   $��2��6�   �� 5      -   5        $��ClearBTN��5      $��Clear Log History��6   $
��Left=�67   $��125.6,432,0,0q�6R   $	��Top=�6i   $	��219��6�   $��41��6�   $��1��6�   $��16(�6�   $��	Agency FB)�6�   $��2��6�   6�   �� 5      -   5        $��
LogTextBox��5      $��Visible=�6   $
��Left=�6R   $	��565��6m   $��	10,10,0,0q�6z   $
 Wrap=�6�   $	��Top=�6�   $	��439��6�   $��2��6�    �� 
IsReadOnly . 6�   �� 5      -	   5        $��
ConnectBTN��5      $��CONNECTION TEST��6   $
��Left=�67   $��123.8,15,0,0q�6R   $	��Top=�6h   $	��221��6�   $��46��6�   $��1��6�   $��2��6�   $��18(�6�   $��	Agency FB)�6�   �� 5      -
   5         ��Name$ 
LoadingBTN��5       J� IsHitTestVisible .  
�������$
��Left=�6J   $��123.8,15,0,0q�6e   $	��Top=�6{   $	��221��6�   $��46��6�   $��1��6�   $��2��6�   .��5      � .+�5      �� "  WpfAnimatedGif.ImageBehavior   AnimatedSource$ GIF/Dual Ring.gif��6   $
��Left=�6A   $��Center=�6\   $��-96,1,0,-0.8q�6w   �� 5      -   5        $��ConditionTXT��5      $��
----------��6    $��Center=�65   $��Center=�6Y   $��-53,1,-64,0.2q�6{   $	��141��6�   $��42��6�   �� 5      -   5        $��ProgressTXT��5      $��0%��6#   $��Right=�6-   $��Center=�6I   $��0,16,-86.4,11.2q�6d   5      5      5       5!      